using Silk.NET.Windowing;
using someui.CrossPlatformHeaderless;

namespace someui;

public static class WindowExtensions
{
    public static void EnableFrameless(this Window window)
    {
        if (!OperatingSystem.IsWindows()) return;
        if (window.NativeHeaderless != null) return;
        
        var bridge = new Win32Headerless(window.SilkWindow, null);
        window.NativeHeaderless = bridge;
    }

    public static void SetMaximizeButton(this Window window, BaseWidget? wdg = null) => 
        window.NativeHeaderless?.SetMaximizeWidget(wdg);
    
    public static void SetHeaderWidget(this Window window, BaseWidget? wdg = null) => 
        window.NativeHeaderless?.SetHeaderWidget(wdg);
}