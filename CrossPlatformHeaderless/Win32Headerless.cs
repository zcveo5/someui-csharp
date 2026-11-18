using System.Runtime.InteropServices;
using Silk.NET.Windowing;

namespace someui.CrossPlatformHeaderless;

#if WINDOWS
public class Win32Headerless : INativeHeaderless
{
    private readonly IntPtr _hwnd;
    private readonly IntPtr _oldWndProcPtr;
    private readonly WNDPROC _customWndProcDelegate; // Держим ссылку, чтобы GC не сожрал
    
    private BaseWidget? _maxiButton;
    private int _bx, _by;
    private BaseWidget? _header;
    private bool _disposed;

    private delegate IntPtr WNDPROC(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    public Win32Headerless(IWindow wnd, BaseWidget? maxiButton)
    {
        var hwnd = wnd.Native!.Win32!.Value.Hwnd;
        
        _hwnd = hwnd;
        _maxiButton = maxiButton;

        UpdateResizePadding();

        // Сабклассим
        _customWndProcDelegate = CustomWndProc;
        
        SetLastError(0);
        _oldWndProcPtr = SetWindowLongPtr(hwnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_customWndProcDelegate));
        
        if (_oldWndProcPtr == IntPtr.Zero && Marshal.GetLastWin32Error() != 0)
        {
            throw new InvalidOperationException("Failed to subclass window.");
        }

        // Пересчитываем рамку
        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, 
            0x0020 /* SWP_FRAMECHANGED */ | 0x0001 | 0x0002 | 0x0004 | 0x0008);
    }

    public void SetHeaderWidget(BaseWidget? wdg) => _header = wdg;
    public void SetMaximizeWidget(BaseWidget? wdg) => _maxiButton = wdg;
    
    private void UpdateResizePadding()
    {
        uint dpi = GetDpiForWindow(_hwnd);
        if (dpi == 0) dpi = 96;

        int padded = GetSystemMetricsForDpi(92 /* SM_CXPADDEDBORDER */, dpi);
        _bx = GetSystemMetricsForDpi(32 /* SM_CXSIZEFRAME */, dpi) + padded;
        _by = GetSystemMetricsForDpi(33 /* SM_CYSIZEFRAME */, dpi) + padded;
    }

    private IntPtr CustomWndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        
        // 1. WM_NCCALCSIZE — убираем стандартную рамку, оставляя отступы под тени/ресайз
        if (msg == 0x0083 /* WM_NCCALCSIZE */ && wParam == (IntPtr)1)
        {
            UpdateResizePadding();

            // rgrc[0] начинает прямо по адресу lParam
            // Читаем RECT напрямую из неуправляемой памяти
            RECT rc = Marshal.PtrToStructure<RECT>(lParam);

            if (IsZoomed(hwnd))
            {
                rc.Left += _bx;
                rc.Top += _by;
                rc.Right -= _bx;
                rc.Bottom -= _by;
            }
            else
            {
                rc.Left += _bx;
                rc.Right -= _bx;
                rc.Bottom -= _by;
            }

            // Записываем измененный RECT обратно в начало lParam
            Marshal.StructureToPtr(rc, lParam, false);

            return IntPtr.Zero;
        }

        // 2. WM_NCHITTEST — определяем зоны ресайза и хит-тест кнопки разворачивания
        if (msg == 0x0084 /* WM_NCHITTEST */)
        {
            int x = (short)(lParam.ToInt32() & 0xffff);
            int y = (short)((lParam.ToInt32() >> 16) & 0xffff);

            GetWindowRect(hwnd, out RECT rect);
            POINT clientPt = new POINT { X = x, Y = y };
            ScreenToClient(hwnd, ref clientPt);

            int zone = 1; // HTCLIENT

            if (_maxiButton != null && _maxiButton.HoveredNow)
            {
                zone = 9; // HTMAXBUTTON
            }
            else if (!IsZoomed(hwnd))
            {
                var left = rect.Left <= x && x < rect.Left + _bx;
                var right = rect.Right - _bx <= x && x < rect.Right;
                var top = rect.Top <= y && y < rect.Top + _by;
                var bottom = rect.Bottom - _by <= y && y < rect.Bottom;

                if (top && left) zone = 13; // HTTOPLEFT
                else if (top && right) zone = 14; // HTTOPRIGHT
                else if (bottom && left) zone = 16; // HTBOTTOMLEFT
                else if (bottom && right) zone = 17; // HTBOTTOMRIGHT
                else if (left) zone = 10; // HTLEFT
                else if (right) zone = 11; // HTRIGHT
                else if (top) zone = 12; // HTTOP
                else if (bottom) zone = 15; // HTBOTTOM
            }

            if (zone != 1)
            {
                CallWindowProc(_oldWndProcPtr, hwnd, 0x0200 /* WM_MOUSEMOVE */, IntPtr.Zero, MAKELPARAM(clientPt.X, clientPt.Y));
            }

            return zone;
        }

        // 3. Перетаскивание за кастомную шапку
        if (msg == 0x0201 /* WM_LBUTTONDOWN */)
        {
            POINT pt = new POINT { X = (short)(lParam.ToInt32() & 0xffff), Y = (short)((lParam.ToInt32() >> 16) & 0xffff) };
            ClientToScreen(hwnd, ref pt);

            if (_header?.HoveredNow ?? false || pt.Y < 1)
            {
                ReleaseCapture();
                SendMessage(hwnd, 0x00A1 /* WM_NCLBUTTONDOWN */, (IntPtr)2 /* HTCAPTION */, MAKELPARAM(pt.X, pt.Y));
                return IntPtr.Zero;
            }
        }

        // 4. Очистка при уничтожении окна
        if (msg == 0x0082 /* WM_NCDESTROY */)
        {
            var result = CallWindowProc(_oldWndProcPtr, hwnd, msg, wParam, lParam);
            Dispose();
            return result;
        }

        return CallWindowProc(_oldWndProcPtr, hwnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (IsWindow(_hwnd))
        {
            SetWindowLongPtr(_hwnd, GWLP_WNDPROC, _oldWndProcPtr);
            SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, 0, 0, 0x0020 | 0x0001 | 0x0002 | 0x0004 | 0x0008);
        }
        GC.SuppressFinalize(this);
    }

    // --- WinAPI структуры и импорты ---
    private const int GWLP_WNDPROC = -4;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)]
    private struct NCCALCSIZE_PARAMS
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public RECT[] rgrc;
    
        // В Windows эта структура содержит указатель на WINDOWPOS в конце.
        // Если его пропустить, смещение массива rgrc внутри памяти сбивается.
        public IntPtr lppos; 
    }

    private static IntPtr MAKELPARAM(int low, int high) => (IntPtr)((high << 16) | (low & 0xFFFF));

    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern int GetSystemMetricsForDpi(int nIndex, uint dpi);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
    [DllImport("user32.dll")] private static extern bool ScreenToClient(IntPtr hwnd, ref POINT lpPoint);
    [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr hwnd, ref POINT lpPoint);
    [DllImport("user32.dll")] private static extern bool IsZoomed(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("kernel32.dll")] private static extern void SetLastError(int dwError);
}

#endif