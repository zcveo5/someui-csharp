using System.Runtime.InteropServices;

namespace someui;



public static class CrossPlatformWait
{
#if WINDOWS
    #warning "System defined as WINDOWS"
    [DllImport("user32.dll")]
    private static extern int MsgWaitForMultipleObjectsEx(uint nCount, IntPtr[] pHandles, uint dwMilliseconds, uint dwWakeMask, uint dwFlags);
    
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr CreateEventW(
        IntPtr lpEventAttributes,
        bool bManualReset,
        bool bInitialState,
        string lpName
    );

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetEvent(IntPtr hEvent);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ResetEvent(IntPtr hEvent);
    
    private static nint _wakeupEvent = CreateEventW(nint.Zero, true, false, "SomeuiWin32Wakeup");
    private static bool _wakeup = false;
    
    public static void WakeupWait()
    {
        SetEvent(_wakeupEvent);
        _wakeup = true;
    }
    public static void Wait()
    {
        MsgWaitForMultipleObjectsEx(0, [_wakeupEvent], 0xFFFFFFFF, 0x04FF, 0x0001);
        if (!_wakeup) return;
        ResetEvent(_wakeupEvent);
        _wakeup = false;
    }

    public static void Dispose()
    {
        CloseHandle(_wakeupEvent);
    }
#elif MACOS
    #warning "System defined as MACOS"

    [DllImport("/System/Library/Frameworks/Foundation.framework/Foundation")]
    private static extern IntPtr NSRunLoop_currentRunLoop();

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation", EntryPoint = "CFRetain")]
    private static extern IntPtr CFRetain(IntPtr obj);

    // Импорт Objective-C runtime
    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_getClass")]
    private static extern IntPtr ObjC_GetClass(string name);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr ObjC_RegisterName(string name);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr ObjC_MsgSend_IntPtr(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr ObjC_MsgSend_Date(IntPtr receiver, IntPtr selector, double seconds);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern bool ObjC_MsgSend_RunMode(IntPtr receiver, IntPtr selector, IntPtr mode, IntPtr date);

    public static void Wait()
    {
        // Получаем константу kCFRunLoopDefaultMode
        IntPtr coreFoundation = NativeLibrary.Load("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation");
        IntPtr nsDefaultRunLoopModePtr = NativeLibrary.GetExport(coreFoundation, "kCFRunLoopDefaultMode");
        IntPtr nsDefaultRunLoopMode = Marshal.ReadIntPtr(nsDefaultRunLoopModePtr);

        IntPtr runLoop = NSRunLoop_currentRunLoop();
        IntPtr nsDateClass = ObjC_GetClass("NSDate");
        IntPtr dateWithTimeIntervalSel = ObjC_RegisterName("dateWithTimeIntervalSinceNow:");
        IntPtr runModeSel = ObjC_RegisterName("runMode:beforeDate:");

        IntPtr laterDate = ObjC_MsgSend_Date(nsDateClass, dateWithTimeIntervalSel, 1024);
        
        ObjC_MsgSend_RunMode(runLoop, runModeSel, nsDefaultRunLoopMode, laterDate);
    }
#else
    #warning "System not definied properly"
    public static void Wait() {}
#endif
}