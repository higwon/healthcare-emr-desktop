using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace HealthNote.Desktop
{
    internal static class WindowSizing
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
        {
            public int Size;
            public NativeRect Monitor;
            public NativeRect Work;
            public int Flags;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

        public static Rect GetWorkArea(Window window)
        {
            MonitorInfo info = new MonitorInfo { Size = Marshal.SizeOf(typeof(MonitorInfo)) };
            IntPtr monitor = MonitorFromWindow(new WindowInteropHelper(window).Handle, 2);
            if (!GetMonitorInfo(monitor, ref info))
            {
                return SystemParameters.WorkArea;
            }

            DpiScale dpi = VisualTreeHelper.GetDpi(window);
            return new Rect(info.Work.Left / dpi.DpiScaleX, info.Work.Top / dpi.DpiScaleY,
                (info.Work.Right - info.Work.Left) / dpi.DpiScaleX,
                (info.Work.Bottom - info.Work.Top) / dpi.DpiScaleY);
        }

        public static Size GetWorkSize(Window window) => GetWorkArea(window).Size;

        public static void Fit(Window window, Size work)
        {
            // Window dimensions include chrome; leave a margin inside the actual monitor work area.
            double width = Math.Max(1, work.Width - 24);
            double height = Math.Max(1, work.Height - 24);
            window.MinWidth = Math.Min(720, width);
            window.MinHeight = Math.Min(520, height);
            window.Width = Math.Min(window.Width, width);
            window.Height = Math.Min(window.Height, height);
        }
    }
}
