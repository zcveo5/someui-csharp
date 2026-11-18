using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SkiaSharp;
using someui.CrossPlatformHeaderless;
using Topten.RichTextKit;

namespace someui;


public unsafe class Window : SceneWidget
{
    public readonly IWindow SilkWindow;
    public INativeHeaderless? NativeHeaderless;
    private bool _isRunning = true;
    
    private readonly List<BaseWidget> _flatRenderList = new();
    private SKSurface? _surface;
    private SKCanvas?_canvas;
    private GRContext? _grContext;
    private SKSurfaceProperties _surfaceProps;
    private bool _isLayoutDirty = true;
    private bool _isSceneDirty = true;
    private bool _isDirty = true;
    private readonly AutoResetEvent _renderSignal = new(false);
    private readonly Thread _renderThread;
    private bool _dirtySurface = true;
    private int _animationCount = 0;

    private bool _headerless;

    public Event Releasing = new();
    
    public readonly Event1Arg<double> AnimationProgress = new();
    
    public Window(string title = "someui", int width = 600, int height = 400, bool headerless = false) : base(null)
    {
        _headerless = headerless;
        RealWindow = this;
        
        ActualWidth = width;
        ActualHeight = height;
        
        var options = WindowOptions.Default;
        options.UpdatesPerSecond = 0;
        options.FramesPerSecond = 0;
        options.Size = new Vector2D<int>(width, height);
        options.Title = title;
        
        SilkWindow = Silk.NET.Windowing.Window.Create(options);

        _surfaceProps = new SKSurfaceProperties(SKSurfacePropsFlags.UseDeviceIndependentFonts, SKPixelGeometry.RgbHorizontal);
    
        SilkWindow.FramebufferResize += (vector) =>
        {
            _dirtySurface = true;
            ActualWidth = vector.X;
            ActualHeight = vector.Y;
            SetSize(vector.X, vector.Y);
            InvalidateScene();
            _renderSignal.Set();
            if (ActualWidth % 5 == 0 || ActualHeight % 5 == 0)
                RebuildScene();
        };

        _renderThread = new Thread(RenderThread)
        {
            Name = $"RenderThread-{Id}",
            IsBackground = true
        };
        
        SilkWindow.Initialize();
        if (_headerless)
            this.EnableFrameless();
        Silk.NET.GLFW.Glfw.GetApi().MakeContextCurrent(null);
        _renderThread.Start();
        
        var input = SilkWindow.CreateInput();
        foreach (var mouse in input.Mice)
        {
            mouse.MouseMove += DispatchMouseHover;
            mouse.MouseUp += DispatchMouseUp;
            mouse.MouseDown += DispatchMouseDown;
        }

        SilkWindow.Render += dt =>
        {
            if (_animationCount > 0)
                AnimationProgress.Invoke(dt);
        };
    }

    public void StartAnimation()
    {
        _animationCount++;
    }

    public void EndAnimation()
    {
        if (_animationCount > 0)
            _animationCount--;
    }
    
    private void DispatchMouseHover(IMouse mouse, Vector2 position)
    {
        var x = position.X;
        var y = position.Y;
        
        foreach (var child in _flatRenderList)
        {
            if (!child.Enabled || !child.Hoverable) continue;
            if (child.Hittest(x, y))
            {
                if (!child.HoveredNow)
                {
                    child.HoveredNow = true;
                    child.MouseEvents.OnHoverEnter.Invoke(x, y);
                }
                child.MouseEvents.OnHover.Invoke(x, y);
            } else if (child.HoveredNow)
            {
                child.HoveredNow = false;
                child.MouseEvents.OnHoverExit.Invoke(x, y);
            }
        }
    }
    
    private void DispatchMouseDown(IMouse mouse, MouseButton btn)
    {
        var x = mouse.Position.X;
        var y = mouse.Position.Y;
        
        foreach (var child in _flatRenderList)
        {
            if (!child.Enabled || !child.Clickable) continue;
            if (!child.Hittest(x, y)) continue;

            if (!child.HeldNow)
                child.HeldNow = true;
            
            switch (btn)
            {
                case MouseButton.Left:
                    child.MouseEvents.OnLeftButtonPress.Invoke(x, y);
                    break;
                case MouseButton.Right:
                    child.MouseEvents.OnRightButtonPress.Invoke(x, y);
                    break;
            }
        }
    }
    
    private void DispatchMouseUp(IMouse mouse, MouseButton btn)
    {
        var x = mouse.Position.X;
        var y = mouse.Position.Y;
        
        foreach (var child in _flatRenderList)
        {
            if (!child.Enabled || !child.Clickable) continue;
            if (!child.Hittest(x, y)) continue;
            
            if (child.HeldNow)
                child.HeldNow = false;
            
            switch (btn)
            {
                case MouseButton.Left:
                    child.MouseEvents.OnLeftButtonRelease.Invoke(x, y);
                    break;
                case MouseButton.Right:
                    child.MouseEvents.OnRightButtonRelease.Invoke(x, y);
                    break;
            }
        }
    }

    private void RenderThread()
    {
        SilkWindow.MakeCurrent();
        SilkWindow.VSync = true;

        var glInterface = GRGlInterface.Create();
        _grContext = GRContext.CreateGl(glInterface);

        UpdateSkiaSurface(SilkWindow.Size.X, SilkWindow.Size.Y);
        
        SilkWindow.Render += RenderWindowContent;
    
        while (_isRunning)
        {
            if (!_isDirty && !_dirtySurface && _animationCount == 0)
            {
                _renderSignal.WaitOne();
                continue;
            }

            if (_dirtySurface)
            {
                _dirtySurface = false;
                UpdateSkiaSurface(SilkWindow.Size.X, SilkWindow.Size.Y);
                _isDirty = true;
            }

            if (!_isDirty && _animationCount == 0) continue;
            
            _isDirty = false;
            SilkWindow.DoRender();
        }

        _grContext?.Dispose();
    }
    
    private void UpdateSkiaSurface(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        var framebufferInfo = new GRGlFramebufferInfo(
            fboId: 0,
            format: SKColorType.Rgba8888.ToGlSizedFormat()
        );

        using var renderTarget = new GRBackendRenderTarget(
            width, height, 0, 8, framebufferInfo
        );

        var newSurface = SKSurface.Create(
            _grContext,
            renderTarget,
            GRSurfaceOrigin.BottomLeft,
            SKColorType.Rgba8888,
            _surfaceProps
        );

        if (newSurface == null)
            return;

        _surface?.Dispose();

        _surface = newSurface;
        _canvas = newSurface.Canvas;
    }

    private void RenderWindowContent(double deltaTime)
    {
        if (_canvas == null) return;
        
        _canvas.Clear();
        
        foreach (var child in _flatRenderList)
        {
            _canvas.Save();
            if (child.AbsoluteClipRect != null)
                _canvas.ClipRect((SKRect)child.AbsoluteClipRect);
            _canvas.Translate(child.ActualGlobalX, child.ActualGlobalY);
            child.DrawSelf(_canvas);
            _canvas.Restore();
        }
        
        //_canvas.DrawLine(0f, 0f, ActualWidth, ActualHeight, WidgetStatic.WhiteDebugPaint);
        //_canvas.DrawLine(ActualWidth, 0f, 0f, ActualHeight, WidgetStatic.WhiteDebugPaint);
        
        _canvas.Flush();
        _grContext?.Flush();
    }
    
    public void InvalidateFlatChildren()
    {
        _isLayoutDirty = true;
        CrossPlatformWait.WakeupWait();
    }
    
    public void InvalidateScene()
    {
        _isSceneDirty = true;
        CrossPlatformWait.WakeupWait();
    }
    
    public void Invalidate()
    {
        _isDirty = true;
        _renderSignal.Set();
    }

    private void RebuildFlatChildren()
    {
        if (!_isLayoutDirty) return;

        _flatRenderList.Clear();
        CollectRecursive(this, _flatRenderList);
        _flatRenderList.Sort((a, b) => a.ZIndex.CompareTo(b.ZIndex));
        _isLayoutDirty = false;
    }

    private static void CollectRecursive(BaseWidget parent, List<BaseWidget> list)
    {
        foreach (var child in parent.Children)
        {
            list.Add(child);
            if (child.Children.Count > 0)
            {
                CollectRecursive(child, list);
            }
        }
    }

    public void Run()
    {
        while (!SilkWindow.IsClosing)
        {
            SilkWindow.DoEvents();
            if (SilkWindow.IsClosing) break;
            SilkWindow.DoUpdate();

            if (_isLayoutDirty)
            {
                RebuildFlatChildren();
                Invalidate();
            }

            if (_isSceneDirty)
            {
                RebuildScene();
                _isSceneDirty = false;
                Invalidate();
            }
            
            CrossPlatformWait.Wait();
        }
        
        CrossPlatformWait.Dispose();
        
        _isRunning = false;
        Invalidate(); // Wake up RenderThread
        _renderThread.Join();
        
        _surface?.Dispose();
        _surface = null;
        _canvas = null;

        _grContext?.Dispose();
        _grContext = null;
        
        SilkWindow.Reset();
        SilkWindow.Dispose();
    }
}