using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace MultiWallFX
{
    public static class TransitionEngine
    {
        private static readonly Random _rng = new Random();
        private static readonly Dictionary<string, HwndSource> _activeTransitions = new Dictionary<string, HwndSource>();
        private static IntPtr _cachedWorkerW = IntPtr.Zero;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            IntPtr lParam,
            uint fuFlags,
            uint uTimeout,
            out IntPtr lpdwResult);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, ref POINT lpPoints, uint cPoints);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

        private const int GWL_STYLE = -16;
        private const int WS_CHILD = 0x40000000;
        private const int WS_POPUP = unchecked((int)0x80000000);
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_CLIPSIBLINGS = 0x04000000;

        private const uint RDW_INVALIDATE = 0x0001;
        private const uint RDW_ERASE = 0x0004;
        private const uint RDW_ALLCHILDREN = 0x0080;
        private const uint RDW_UPDATENOW = 0x0100;

        /// <summary>
        /// Finds or spawns the WorkerW window located behind the desktop icons and behind all open applications.
        /// </summary>
        public static IntPtr GetWorkerW()
        {
            if (_cachedWorkerW != IntPtr.Zero && IsWindow(_cachedWorkerW))
            {
                return _cachedWorkerW;
            }

            IntPtr progman = FindWindow("Progman", null);
            if (progman == IntPtr.Zero)
            {
                App.Log("GetWorkerW: Progman not found");
                return IntPtr.Zero;
            }

            IntPtr result = IntPtr.Zero;
            // Send 0x052C to Progman. This spawns the WorkerW windows.
            SendMessageTimeout(progman, 0x052C, new IntPtr(0xD), new IntPtr(1), 0, 1000, out result);

            IntPtr workerw = IntPtr.Zero;

            // WorkerW is the sibling immediately behind the window containing SHELLDLL_DefView
            EnumWindows((topHandle, lParam) =>
            {
                IntPtr defView = FindWindowEx(topHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (defView != IntPtr.Zero)
                {
                    workerw = FindWindowEx(IntPtr.Zero, topHandle, "WorkerW", null);
                }
                return true;
            }, IntPtr.Zero);

            if (workerw == IntPtr.Zero)
            {
                // Fallback attempt: try 0x052C with (0, 0)
                SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero, 0, 1000, out result);
                EnumWindows((topHandle, lParam) =>
                {
                    IntPtr defView = FindWindowEx(topHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (defView != IntPtr.Zero)
                    {
                        workerw = FindWindowEx(IntPtr.Zero, topHandle, "WorkerW", null);
                    }
                    return true;
                }, IntPtr.Zero);
            }

            // Fallback: search for top-level WorkerW that does NOT contain SHELLDLL_DefView
            if (workerw == IntPtr.Zero)
            {
                EnumWindows((topHandle, lParam) =>
                {
                    var sb = new StringBuilder(256);
                    GetClassName(topHandle, sb, sb.Capacity);
                    if (sb.ToString() == "WorkerW")
                    {
                        IntPtr defView = FindWindowEx(topHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                        if (defView == IntPtr.Zero)
                        {
                            workerw = topHandle;
                            return false;
                        }
                    }
                    return true;
                }, IntPtr.Zero);
            }

            _cachedWorkerW = workerw;
            App.Log("GetWorkerW: found WorkerW=" + workerw);
            return workerw;
        }

        public static IEasingFunction CreateEasing(string easingType)
        {
            switch (easingType)
            {
                case "Quadratic":
                    return new QuadraticEase { EasingMode = EasingMode.EaseInOut };
                case "Sine":
                    return new SineEase { EasingMode = EasingMode.EaseInOut };
                case "Bounce":
                    return new BounceEase { EasingMode = EasingMode.EaseOut, Bounces = 2, Bounciness = 2 };
                case "Linear":
                    return null;
                case "Cubic":
                default:
                    return new CubicEase { EasingMode = EasingMode.EaseInOut };
            }
        }

        public static BitmapImage LoadBitmapSafe(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(Path.GetFullPath(path));
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        public static string GetWindowsCachedWallpaperPath(Rectangle screenBounds)
        {
            try
            {
                string themesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Themes");
                string cachedFilesDir = Path.Combine(themesDir, "CachedFiles");
                if (Directory.Exists(cachedFilesDir))
                {
                    string pattern = string.Format("CachedImage_{0}_{1}_*.jpg", screenBounds.Width, screenBounds.Height);
                    string[] files = Directory.GetFiles(cachedFilesDir, pattern);
                    if (files.Length > 0 && File.Exists(files[0]))
                    {
                        return files[0];
                    }
                    string[] anyFiles = Directory.GetFiles(cachedFilesDir, "*.jpg");
                    if (anyFiles.Length > 0 && File.Exists(anyFiles[0]))
                    {
                        return anyFiles[0];
                    }
                }
                string transcoded = Path.Combine(themesDir, "TranscodedWallpaper");
                if (File.Exists(transcoded))
                {
                    return transcoded;
                }
            }
            catch { }
            return null;
        }

        public static FrameworkElement CreateAlignedWallpaperElement(BitmapImage bmp, Rectangle screenBounds)
        {
            if (bmp == null) return new Grid { Width = screenBounds.Width, Height = screenBounds.Height };

            int screenW = screenBounds.Width;
            int screenH = screenBounds.Height;

            double imgW = bmp.PixelWidth;
            double imgH = bmp.PixelHeight;

            // If already exact screen resolution (such as Windows CachedImage 1920x1080), display 1:1
            if (bmp.PixelWidth == screenW && bmp.PixelHeight == screenH)
            {
                return new System.Windows.Controls.Image
                {
                    Source = bmp,
                    Width = screenW,
                    Height = screenH,
                    Stretch = Stretch.Fill
                };
            }

            if (imgW <= 0 || imgH <= 0)
            {
                return new System.Windows.Controls.Image
                {
                    Source = bmp,
                    Width = screenW,
                    Height = screenH,
                    Stretch = Stretch.UniformToFill
                };
            }

            // Uniform fill scaling
            double scale = Math.Max((double)screenW / imgW, (double)screenH / imgH);
            double scaledW = imgW * scale;
            double scaledH = imgH * scale;

            double excessW = scaledW - screenW;
            double excessH = scaledH - screenH;

            // Detect actual Taskbar height on this screen to match Windows wallpaper alignment
            double taskbarOffset = 40.0;
            try
            {
                IntPtr trayWnd = FindWindow("Shell_TrayWnd", null);
                if (trayWnd != IntPtr.Zero)
                {
                    RECT rTray;
                    if (GetWindowRect(trayWnd, out rTray))
                    {
                        int h = rTray.Bottom - rTray.Top;
                        if (h > 0 && h < 200)
                        {
                            taskbarOffset = h;
                        }
                    }
                }
            }
            catch { }

            // Windows Desktop Wallpaper Fill Alignment:
            // Windows aligns the wallpaper center with the visible workspace above the bottom taskbar.
            double topCrop = excessH > 0 ? Math.Max(0, (excessH - taskbarOffset) / 2.0) : 0;
            double leftCrop = excessW > 0 ? (excessW / 2.0) : 0;

            var canvas = new Canvas
            {
                Width = screenW,
                Height = screenH,
                ClipToBounds = true
            };

            var img = new System.Windows.Controls.Image
            {
                Source = bmp,
                Width = scaledW,
                Height = scaledH,
                Stretch = Stretch.Fill
            };

            Canvas.SetLeft(img, -leftCrop);
            Canvas.SetTop(img, -topCrop);
            canvas.Children.Add(img);

            return canvas;
        }

        public static void ExecuteTransition(
            Rectangle screenBounds,
            string devicePath,
            string oldImagePath,
            string newImagePath,
            string effectType,
            double durationMs,
            string easingType,
            Action onComplete = null)
        {
            if (System.Windows.Application.Current != null && !System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    ExecuteTransition(screenBounds, devicePath, oldImagePath, newImagePath, effectType, durationMs, easingType, onComplete);
                }));
                return;
            }

            if (string.IsNullOrEmpty(newImagePath) || !File.Exists(newImagePath))
            {
                if (onComplete != null) onComplete();
                return;
            }

            if (effectType == "None" || durationMs <= 50)
            {
                WallpaperManager.SetWallpaper(devicePath, newImagePath);
                if (onComplete != null) onComplete();
                return;
            }

            if (effectType == "Random")
            {
                string[] effects = new string[] { "Fade", "SlideLeft", "SlideRight", "SlideUp", "SlideDown", "Zoom" };
                effectType = effects[_rng.Next(effects.Length)];
            }

            var newBmp = LoadBitmapSafe(newImagePath);
            if (newBmp == null)
            {
                WallpaperManager.SetWallpaper(devicePath, newImagePath);
                if (onComplete != null) onComplete();
                return;
            }

            // Prioritize the Windows cached wallpaper file for the monitor to ensure 100% pixel-perfect match at transition start
            BitmapImage oldBmp = null;
            string currentCached = GetWindowsCachedWallpaperPath(screenBounds);
            if (!string.IsNullOrEmpty(currentCached) && File.Exists(currentCached))
            {
                oldBmp = LoadBitmapSafe(currentCached);
            }
            if (oldBmp == null)
            {
                oldBmp = LoadBitmapSafe(oldImagePath);
            }
            if (oldBmp == null)
            {
                string currentWp = WallpaperManager.GetCurrentWallpaper(devicePath);
                oldBmp = LoadBitmapSafe(currentWp);
            }
            if (oldBmp == null)
            {
                oldBmp = newBmp;
            }

            IntPtr workerw = GetWorkerW();
            if (workerw == IntPtr.Zero)
            {
                App.Log("WorkerW not available. Applying wallpaper directly without overlay.");
                WallpaperManager.SetWallpaper(devicePath, newImagePath);
                if (onComplete != null) onComplete();
                return;
            }

            // Convert screen coordinates to WorkerW client coordinates
            POINT pt = new POINT { x = screenBounds.Left, y = screenBounds.Top };
            MapWindowPoints(IntPtr.Zero, workerw, ref pt, 1);

            // Clean up any existing active transition for this device
            if (_activeTransitions.ContainsKey(devicePath))
            {
                try
                {
                    var oldSource = _activeTransitions[devicePath];
                    _activeTransitions.Remove(devicePath);
                    oldSource.Dispose();
                }
                catch { }
            }

            HwndSource source = null;
            try
            {
                var parameters = new HwndSourceParameters("MultiWallFX_Transition")
                {
                    PositionX = pt.x,
                    PositionY = pt.y,
                    Width = screenBounds.Width,
                    Height = screenBounds.Height,
                    WindowStyle = WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS,
                    ParentWindow = workerw
                };

                source = new HwndSource(parameters);
            }
            catch (Exception ex)
            {
                App.Log("Failed to create HwndSource in WorkerW: " + ex.Message);
                WallpaperManager.SetWallpaper(devicePath, newImagePath);
                if (onComplete != null) onComplete();
                return;
            }

            _activeTransitions[devicePath] = source;

            var grid = new Grid
            {
                Width = screenBounds.Width,
                Height = screenBounds.Height,
                ClipToBounds = true,
                Background = System.Windows.Media.Brushes.Black
            };

            var elemOld = CreateAlignedWallpaperElement(oldBmp, screenBounds);
            var elemNew = CreateAlignedWallpaperElement(newBmp, screenBounds);

            grid.Children.Add(elemOld);
            grid.Children.Add(elemNew);
            source.RootVisual = grid;

            DateTime startTime = DateTime.Now;
            WallpaperManager.SetWallpaper(devicePath, newImagePath);

            var duration = TimeSpan.FromMilliseconds(durationMs);
            var easing = CreateEasing(easingType);

            bool isFinished = false;
            Action finish = () =>
            {
                if (isFinished) return;
                isFinished = true;

                double elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                int waitMs = Math.Max(150, (int)(500 - elapsed));

                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(waitMs);
                timer.Tick += (s2, e2) =>
                {
                    timer.Stop();
                    try
                    {
                        if (_activeTransitions.ContainsKey(devicePath) && _activeTransitions[devicePath] == source)
                        {
                            _activeTransitions.Remove(devicePath);
                        }
                        source.Dispose();
                        RedrawWindow(workerw, IntPtr.Zero, IntPtr.Zero, RDW_INVALIDATE | RDW_ERASE | RDW_UPDATENOW | RDW_ALLCHILDREN);
                    }
                    catch (Exception ex)
                    {
                        App.Log("Error disposing transition source: " + ex.Message);
                    }
                    if (onComplete != null) onComplete();
                };
                timer.Start();
            };

            switch (effectType)
            {
                case "SlideLeft":
                    {
                        var translate = new TranslateTransform(screenBounds.Width, 0);
                        elemNew.RenderTransform = translate;
                        var anim = new DoubleAnimation(screenBounds.Width, 0, duration) { EasingFunction = easing };
                        anim.Completed += (s2, e2) => finish();
                        translate.BeginAnimation(TranslateTransform.XProperty, anim);
                        break;
                    }

                case "SlideRight":
                    {
                        var translate = new TranslateTransform(-screenBounds.Width, 0);
                        elemNew.RenderTransform = translate;
                        var anim = new DoubleAnimation(-screenBounds.Width, 0, duration) { EasingFunction = easing };
                        anim.Completed += (s2, e2) => finish();
                        translate.BeginAnimation(TranslateTransform.XProperty, anim);
                        break;
                    }

                case "SlideUp":
                    {
                        var translate = new TranslateTransform(0, screenBounds.Height);
                        elemNew.RenderTransform = translate;
                        var anim = new DoubleAnimation(screenBounds.Height, 0, duration) { EasingFunction = easing };
                        anim.Completed += (s2, e2) => finish();
                        translate.BeginAnimation(TranslateTransform.YProperty, anim);
                        break;
                    }

                case "SlideDown":
                    {
                        var translate = new TranslateTransform(0, -screenBounds.Height);
                        elemNew.RenderTransform = translate;
                        var anim = new DoubleAnimation(-screenBounds.Height, 0, duration) { EasingFunction = easing };
                        anim.Completed += (s2, e2) => finish();
                        translate.BeginAnimation(TranslateTransform.YProperty, anim);
                        break;
                    }

                case "Zoom":
                    {
                        elemNew.Opacity = 0.0;
                        var scale = new ScaleTransform(1.10, 1.10);
                        elemNew.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                        elemNew.RenderTransform = scale;

                        var scaleAnimX = new DoubleAnimation(1.10, 1.0, duration) { EasingFunction = easing };
                        var scaleAnimY = new DoubleAnimation(1.10, 1.0, duration) { EasingFunction = easing };
                        var opacityAnim = new DoubleAnimation(0.0, 1.0, duration) { EasingFunction = easing };

                        opacityAnim.Completed += (s2, e2) => finish();

                        scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimX);
                        scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimY);
                        elemNew.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
                        break;
                    }

                case "Fade":
                default:
                    {
                        elemNew.Opacity = 0.0;
                        var anim = new DoubleAnimation(0.0, 1.0, duration) { EasingFunction = easing };
                        anim.Completed += (s2, e2) => finish();
                        elemNew.BeginAnimation(UIElement.OpacityProperty, anim);
                        break;
                    }
            }
        }
    }
}
