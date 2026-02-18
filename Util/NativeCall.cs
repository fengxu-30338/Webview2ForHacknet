using System;
using System.Runtime.InteropServices;

namespace Webview2ForHacknet.Util
{
    internal static class NativeCall
    {
        public const int LOGPIXELSX = 88;
        public const int LOGPIXELSY = 90;

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern uint GetDpiForWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern IntPtr ReleaseDC(IntPtr hwnd, IntPtr hdcHandle);

        [DllImport("gdi32.dll")]
        public static extern int GetDeviceCaps(IntPtr hdcHandle, int nIndex);

        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus(IntPtr hWnd);

    }
}
