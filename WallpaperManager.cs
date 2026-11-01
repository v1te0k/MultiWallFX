using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MultiWallFX
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public enum DesktopWallpaperPosition
    {
        Center = 0,
        Tile = 1,
        Stretch = 2,
        Fit = 3,
        Fill = 4,
        Span = 5
    }

    [ComImport]
    [Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDesktopWallpaper
    {
        void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID);
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetMonitorDevicePathAt(uint monitorIndex);
        [return: MarshalAs(UnmanagedType.U4)]
        uint GetMonitorDevicePathCount();
        [return: MarshalAs(UnmanagedType.Struct)]
        RECT GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorID);
        void SetBackgroundColor([MarshalAs(UnmanagedType.U4)] uint color);
        [return: MarshalAs(UnmanagedType.U4)]
        uint GetBackgroundColor();
        void SetPosition([MarshalAs(UnmanagedType.I4)] DesktopWallpaperPosition position);
        [return: MarshalAs(UnmanagedType.I4)]
        DesktopWallpaperPosition GetPosition();
        void SetSlideshow(IntPtr items);
        IntPtr GetSlideshow();
        void AdvanceSlideshow([MarshalAs(UnmanagedType.LPWStr)] string monitorID, [MarshalAs(UnmanagedType.I4)] int direction);
        int GetStatus();
        bool Enable();
    }

    [ComImport]
    [Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD")]
    public class DesktopWallpaperClass
    {
    }

    public class MonitorInfo
    {
        public int Index { get; set; }
        public string DevicePath { get; set; }
        public string DisplayName { get; set; }
        public Rectangle Bounds { get; set; }
        public bool IsPrimary { get; set; }
        public string CurrentWallpaper { get; set; }
    }

    public static class WallpaperManager
    {
        private static IDesktopWallpaper _wallpaperService;

        private static IDesktopWallpaper GetService()
        {
            if (_wallpaperService == null)
            {
                _wallpaperService = (IDesktopWallpaper)new DesktopWallpaperClass();
            }
            return _wallpaperService;
        }

        public static List<MonitorInfo> GetAllMonitors()
        {
            var list = new List<MonitorInfo>();
            try
            {
                var service = GetService();
                uint count = service.GetMonitorDevicePathCount();
                var screens = Screen.AllScreens;

                for (uint i = 0; i < count; i++)
                {
                    string path = service.GetMonitorDevicePathAt(i);
                    string currentWp = "";
                    try
                    {
                        currentWp = service.GetWallpaper(path);
                    }
                    catch { }

                    RECT r = new RECT();
                    try
                    {
                        r = service.GetMonitorRECT(path);
                    }
                    catch { }

                    var rect = new Rectangle(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
                    if (rect.Width <= 0 || rect.Height <= 0)
                    {
                        if (i < screens.Length)
                        {
                            rect = screens[i].Bounds;
                        }
                    }

                    bool primary = false;
                    string name = "Monitor " + (i + 1);
                    if (i < screens.Length)
                    {
                        primary = screens[i].Primary;
                        name = (primary ? "Monitor " + (i + 1) + " (Primary)" : "Monitor " + (i + 1)) +
                               " [" + rect.Width + "x" + rect.Height + "]";
                    }

                    list.Add(new MonitorInfo
                    {
                        Index = (int)i,
                        DevicePath = path,
                        DisplayName = name,
                        Bounds = rect,
                        IsPrimary = primary,
                        CurrentWallpaper = currentWp
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error enumerating monitors: " + ex.Message);
            }

            // Fallback if COM returned 0 monitors
            if (list.Count == 0)
            {
                var screens = Screen.AllScreens;
                for (int i = 0; i < screens.Length; i++)
                {
                    list.Add(new MonitorInfo
                    {
                        Index = i,
                        DevicePath = "MONITOR_" + i,
                        DisplayName = "Monitor " + (i + 1) + (screens[i].Primary ? " (Primary)" : "") +
                                      " [" + screens[i].Bounds.Width + "x" + screens[i].Bounds.Height + "]",
                        Bounds = screens[i].Bounds,
                        IsPrimary = screens[i].Primary,
                        CurrentWallpaper = ""
                    });
                }
            }

            return list;
        }

        public static bool SetWallpaper(string monitorId, string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath)) return false;

            try
            {
                var service = GetService();
                service.SetWallpaper(monitorId, imagePath);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error setting wallpaper: " + ex.Message);
                return false;
            }
        }

        public static string GetCurrentWallpaper(string monitorId)
        {
            try
            {
                var service = GetService();
                return service.GetWallpaper(monitorId);
            }
            catch
            {
                return "";
            }
        }
    }
}
