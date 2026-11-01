using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace MultiWallFX
{
    public static class FullscreenDetector
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);
        private const uint GA_ROOT = 2;

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);
        private const int DWMWA_CLOAKED = 14;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public static bool IsFullscreenAppRunning()
        {
            return IsExclusiveFullscreenRunning();
        }

        public static bool IsExclusiveFullscreenRunning()
        {
            try
            {
                IntPtr fgWindow = GetForegroundWindow();
                if (fgWindow == IntPtr.Zero) return false;
                if (fgWindow == GetDesktopWindow() || fgWindow == GetShellWindow()) return false;

                // Ignore if foreground is MultiWallFX
                uint fgPid;
                GetWindowThreadProcessId(fgWindow, out fgPid);
                if (fgPid == (uint)System.Diagnostics.Process.GetCurrentProcess().Id)
                    return false;

                var sb = new StringBuilder(64);
                GetClassName(fgWindow, sb, sb.Capacity);
                string cls = sb.ToString();
                if (cls == "Progman" || cls == "WorkerW" || cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd")
                    return false;

                // Check if cloaked
                int cloaked = 0;
                if (DwmGetWindowAttribute(fgWindow, DWMWA_CLOAKED, out cloaked, 4) == 0 && cloaked != 0)
                    return false;

                RECT rect;
                if (!GetWindowRect(fgWindow, out rect)) return false;

                foreach (var screen in Screen.AllScreens)
                {
                    if (rect.Left <= screen.Bounds.Left &&
                        rect.Top <= screen.Bounds.Top &&
                        rect.Right >= screen.Bounds.Right &&
                        rect.Bottom >= screen.Bounds.Bottom)
                    {
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        public static bool IsScreenCovered(Rectangle screenBounds)
        {
            try
            {
                IntPtr fgWindow = GetForegroundWindow();
                if (fgWindow == IntPtr.Zero) return false;
                if (fgWindow == GetDesktopWindow() || fgWindow == GetShellWindow()) return false;

                // If foreground window belongs to MultiWallFX, the user is looking at/testing the app - do NOT consider covered
                uint fgPid;
                GetWindowThreadProcessId(fgWindow, out fgPid);
                if (fgPid == (uint)System.Diagnostics.Process.GetCurrentProcess().Id)
                {
                    return false;
                }

                // Check the foreground window or its top-level root window
                IntPtr targetWindow = fgWindow;
                IntPtr rootWindow = GetAncestor(fgWindow, GA_ROOT);
                if (rootWindow != IntPtr.Zero)
                {
                    targetWindow = rootWindow;
                }

                if (IsWindowCoveringScreen(targetWindow, screenBounds))
                {
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsWindowCoveringScreen(IntPtr hWnd, Rectangle screenBounds)
        {
            if (hWnd == IntPtr.Zero) return false;
            if (hWnd == GetDesktopWindow() || hWnd == GetShellWindow()) return false;
            if (IsIconic(hWnd)) return false;

            // Check if cloaked (invisible UWP background window)
            int cloaked = 0;
            if (DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out cloaked, 4) == 0 && cloaked != 0)
            {
                return false;
            }

            var sb = new StringBuilder(64);
            GetClassName(hWnd, sb, sb.Capacity);
            string cls = sb.ToString();

            if (cls == "Progman" || cls == "WorkerW" || cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd")
            {
                return false;
            }

            RECT rect;
            if (!GetWindowRect(hWnd, out rect)) return false;

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            if (width <= 0 || height <= 0) return false;

            // Case 1: Exclusive Fullscreen (covers entire monitor bounds)
            if (rect.Left <= screenBounds.Left &&
                rect.Top <= screenBounds.Top &&
                rect.Right >= screenBounds.Right &&
                rect.Bottom >= screenBounds.Bottom)
            {
                return true;
            }

            // Find matching screen for WorkingArea check
            Screen targetScreen = null;
            foreach (var s in Screen.AllScreens)
            {
                if (s.Bounds == screenBounds)
                {
                    targetScreen = s;
                    break;
                }
            }
            if (targetScreen == null) targetScreen = Screen.PrimaryScreen;

            // Case 2: Window is Maximized on this screen
            if (IsZoomed(hWnd))
            {
                int centerX = (rect.Left + rect.Right) / 2;
                int centerY = (rect.Top + rect.Bottom) / 2;
                if (screenBounds.Contains(centerX, centerY))
                {
                    return true;
                }
            }

            // Case 3: Borderless window covering the whole WorkingArea
            var wa = targetScreen.WorkingArea;
            if (rect.Left <= wa.Left &&
                rect.Top <= wa.Top &&
                rect.Right >= wa.Right &&
                rect.Bottom >= wa.Bottom)
            {
                return true;
            }

            return false;
        }
    }
}
