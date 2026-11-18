using SkiaSharp;

namespace someui;

internal static class WidgetStatic
{
    private static int _widgetId;

    public static SKPaint WhiteDebugPaint = new SKPaint() { Color = SKColors.White };
    public static SKFont DebugFont = new SKFont(SKTypeface.Default, 14);

    public static int GetId()
    {
        _widgetId++;
        return _widgetId;
    }
}

public struct MouseEvents
{
    public MouseEvents()
    { }
    
    public EventPairArg<float, float> OnLeftButtonPress = new();
    public EventPairArg<float, float> OnLeftButtonRelease = new();
    
    public EventPairArg<float, float> OnRightButtonPress = new();
    public EventPairArg<float, float> OnRightButtonRelease = new();
    
    public EventPairArg<float, float> OnHoverEnter = new();
    public EventPairArg<float, float> OnHover = new();
    public EventPairArg<float, float> OnHoverExit = new();
}

public class BaseWidget // Bind enabled
{
    public bool Debug = false;
    public bool Enabled = true;
    // Flags for event dispatching
    public bool Clickable = true;
    public bool Hoverable = true;
    public bool Scrollable = false;

    public bool HoveredNow = false;
    public bool HeldNow = false;
    
    public readonly int Id = WidgetStatic.GetId();
    public string Name = "";
    public readonly BaseWidget? Parent;
    public readonly BaseWidget Window;
    public Window? RealWindow;

    private readonly List<BaseWidget> _children = [];

    public IReadOnlyList<BaseWidget> Children => _children;

    public float RequiredX      {get; protected set;} = 0;
    public float RequiredY      {get; protected set;} = 0;
    public float RequiredWidth  {get; protected set;} = 0;
    public float RequiredHeight {get; protected set;} = 0;
    
    public float ActualX      = 0;
    public float ActualY      = 0;
    public float ActualWidth  = 0;
    public float ActualHeight = 0;
    
    public float ActualGlobalX { get; private set; } = 0;
    public float ActualGlobalY { get; private set; } = 0;

    public Event OnResize = new();
    public Event OnRepos = new();

    public MouseEvents MouseEvents = new();

    public int ZIndex = 0;
    
    public SKRect? AbsoluteClipRect { get; protected set; }
    
    public BaseWidget(BaseWidget? parent)
    {
        Name = $"{GetType()}-{Id}";
        
        Parent = parent;
        Window = parent == null ? this : parent.Window;
        RealWindow = parent?.RealWindow ?? null;

        Parent?.AddChild(this);
        
        Parent?.OnResize.Add(RebuildGlobal);

        if (Parent != null)
        {
            ZIndex = Parent.ZIndex + 1;
        }
    }

    public void SetZ(int newZIndex)
    {
        ZIndex = newZIndex;
        RealWindow?.InvalidateFlatChildren();
    }

    public override string ToString()
    {
        return $"<Widget {Name}>";
    }

    public void SetActualSizes(float x, float y, float width, float height)
    {
        ActualX = x;
        ActualY = y;
        ActualWidth = width;
        ActualHeight = height;
        RebuildGlobal();
    }

    private void RebuildGlobal()
    {
        if (Parent == null) return;
        
        ActualGlobalX = Parent.ActualGlobalX + ActualX;
        ActualGlobalY = Parent.ActualGlobalY + ActualY;
        
        var localClip = new SKRect(0, 0, ActualWidth, ActualHeight);
        
        var globalClip = new SKRect(
            ActualGlobalX + localClip.Left,
            ActualGlobalY + localClip.Top,
            ActualGlobalX + localClip.Right,
            ActualGlobalY + localClip.Bottom
        );
        
        if (Parent?.AbsoluteClipRect is SKRect parentClip)
        {
            var clipped = SKRect.Intersect(parentClip, globalClip);
            AbsoluteClipRect = clipped;
        }
        else
        {
            AbsoluteClipRect = globalClip;
        }
    }

    public void Destroy()
    {
        Parent?.RemoveChild(this);
    }
    
    protected virtual bool CanAddChild(BaseWidget child)
        => true;

    public void AddChild(BaseWidget widget)
    {
        if (!CanAddChild(widget))
            throw new InvalidOperationException(
                $"{GetType().Name} cannot contain {widget.GetType().Name}");
        
        if (widget == this)
            throw new InvalidOperationException(
                "A widget cannot be its own child.");

        if (_children.Contains(widget))
            throw new InvalidOperationException(
                "Widget is already a child.");

        _children.Add(widget);
        RealWindow?.InvalidateFlatChildren();
        RealWindow?.Invalidate();
    }
    
    public void RemoveChild(BaseWidget widget)
    {
        _children.Remove(widget);
    }

    public void SetSize(float width, float height)
    {
        RequiredWidth = width;
        RequiredHeight = height;
        OnResize.Invoke();
    }
    
    public void SetPosition(float x, float y)
    {
        RequiredX = x;
        RequiredY = y;
        OnRepos.Invoke();
    }

    public virtual bool Hittest(float x, float y)
    {
        return ActualGlobalX <= x && x <= ActualGlobalX + ActualWidth &&
               ActualGlobalY <= y && y <= ActualGlobalY + ActualHeight;
    }

    public virtual void DrawSelf(SkiaSharp.SKCanvas ctx)
    { }
    
}