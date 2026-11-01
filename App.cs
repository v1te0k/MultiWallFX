using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace MultiWallFX
{
    public class App : System.Windows.Application
    {
        public static App Instance { get; private set; }

        private static Mutex _appMutex;
        private AppConfig _config;
        private MainWindow _mainWindow;
        private NotifyIcon _trayIcon;
        private DispatcherTimer _slideshowTimer;
        private Random _rng = new Random();
        private bool _isPaused = false;
        private MenuItem _menuPauseItem;

        public static void Log(string msg)
        {
            try
            {
                string log = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_debug.log");
                File.AppendAllText(log, "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + msg + "\n");
            }
            catch { }
        }

        [STAThread]
        public static void Main(string[] args)
        {
            Log("Main entered");
            try
            {
                bool createdNew;
                _appMutex = new Mutex(true, "MultiWallFX_Mutex_" + Environment.UserName, out createdNew);
                Log("Mutex createdNew=" + createdNew);
                if (!createdNew)
                {
                    Log("App already running, exiting");
                    return;
                }

                var app = new App();
                app.RunApp(args);
            }
            catch (Exception ex)
            {
                Log("FATAL: " + ex);
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        private void RunApp(string[] args)
        {
            try
            {
                Log("RunApp started");
                IntPtr progmanHwnd = FindWindow("Progman", null);
                Log("Progman handle from inside app: " + progmanHwnd);
                Instance = this;
                ShutdownMode = ShutdownMode.OnExplicitShutdown;

                Log("Loading config...");
                _config = AppConfig.Load();
                Log("Config loaded");

                // Setup Tray Icon
                InitTrayIcon();
                Log("Tray icon initialized");

                // Setup Slideshow Timer
                _slideshowTimer = new DispatcherTimer(DispatcherPriority.Normal);
                _slideshowTimer.Tick += OnSlideshowTick;
                UpdateTimerInterval();
                Log("Timer initialized with DispatcherPriority.Normal");

                bool isStartupArg = false;
                foreach (var arg in args)
                {
                    if (arg.Equals("-startup", StringComparison.OrdinalIgnoreCase))
                    {
                        isStartupArg = true;
                        break;
                    }
                }

                if (isStartupArg)
                {
                    Log("Startup mode branch");
                    if (_config.RandomizeOnStartup)
                    {
                        TriggerNextWallpapers(false);
                    }

                    if (_config.LockScreen.Enabled && _config.LockScreen.RandomizeOnStartup)
                    {
                        TriggerNextLockScreen();
                    }

                    if (_config.EnableSlideshow)
                    {
                        _slideshowTimer.Start();
                    }
                    else
                    {
                        Shutdown();
                        return;
                    }
                }
                else
                {
                    Log("Normal mode branch - creating MainWindow");
                    if (_config.EnableSlideshow)
                    {
                        _slideshowTimer.Start();
                    }

                    _mainWindow = new MainWindow(_config);
                    Log("MainWindow created, showing...");
                    _mainWindow.Show();
                    Log("MainWindow shown");
                    UpdateTimerInterval();
                }

                Log("Calling Run()...");
                Run();
                Log("Run() returned");
            }
            catch (Exception ex)
            {
                Log("RunApp EXCEPTION: " + ex);
            }
        }

        private void InitTrayIcon()
        {
            _trayIcon = new NotifyIcon();
            _trayIcon.Text = "MultiWall FX - Wallpaper & Lock Screen";

            // Extract default application icon or system icon
            try
            {
                string icoFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(icoFile))
                {
                    _trayIcon.Icon = new Icon(icoFile);
                }
                else
                {
                    _trayIcon.Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
                }
            }
            catch
            {
                _trayIcon.Icon = SystemIcons.Application;
            }

            _trayIcon.Visible = true;

            var menu = new ContextMenu();

            var openItem = new MenuItem("Open MultiWall FX", (s, e) => ShowMainWindow());
            openItem.DefaultItem = true;
            menu.MenuItems.Add(openItem);

            menu.MenuItems.Add(new MenuItem("⚡ Change Wallpaper Now (Random)", (s, e) =>
            {
                TriggerNextWallpapers(true);
                if (_config.RandomInterval) UpdateTimerInterval();
            }));
            menu.MenuItems.Add(new MenuItem("🔒 Change Lock Screen Now", (s, e) => TriggerNextLockScreen()));

            menu.MenuItems.Add(new MenuItem("-"));

            _menuPauseItem = new MenuItem("⏸️ Pause Slideshow", (s, e) => TogglePause());
            menu.MenuItems.Add(_menuPauseItem);

            menu.MenuItems.Add(new MenuItem("-"));

            menu.MenuItems.Add(new MenuItem("❌ Exit", (s, e) =>
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                Shutdown();
            }));

            _trayIcon.ContextMenu = menu;
            _trayIcon.DoubleClick += (s, e) => ShowMainWindow();
        }

        public void ShowMainWindow()
        {
            if (_mainWindow == null)
            {
                _mainWindow = new MainWindow(_config);
            }
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
        }

        public void ShowTrayNotification(string message)
        {
            if (_trayIcon != null)
            {
                _trayIcon.ShowBalloonTip(2000, "MultiWall FX", message, ToolTipIcon.Info);
            }
        }

        public static string FormatDuration(int seconds)
        {
            if (seconds < 60) return seconds + " seconds";
            if (seconds < 3600)
            {
                int m = seconds / 60;
                int s = seconds % 60;
                return s > 0 ? (m + "m " + s + "s") : (m + " min");
            }
            int h = seconds / 3600;
            int remM = (seconds % 3600) / 60;
            return remM > 0 ? (h + "h " + remM + "m") : (h + " hrs");
        }

        public void UpdateTimerInterval()
        {
            if (_slideshowTimer != null)
            {
                int sec;
                if (_config.RandomInterval)
                {
                    int minSec = Math.Max(5, _config.RandomIntervalMinSeconds);
                    int maxSec = Math.Max(minSec + 1, _config.RandomIntervalMaxSeconds);
                    sec = _rng.Next(minSec, maxSec + 1);
                }
                else
                {
                    sec = Math.Max(5, _config.IntervalSeconds);
                }

                _slideshowTimer.Interval = TimeSpan.FromSeconds(sec);

                if (_config.EnableSlideshow && !_isPaused)
                {
                    if (!_slideshowTimer.IsEnabled) _slideshowTimer.Start();
                }
                else
                {
                    _slideshowTimer.Stop();
                }

                Log(string.Format("UpdateTimerInterval: nextSec={0}, random={1}, slideshowEnabled={2}, isPaused={3}, timerRunning={4}",
                    sec, _config.RandomInterval, _config.EnableSlideshow, _isPaused, _slideshowTimer.IsEnabled));

                string durationStr = FormatDuration(sec);
                if (_mainWindow != null)
                {
                    if (_config.RandomInterval)
                    {
                        _mainWindow.UpdateStatus("Slideshow: Random Time & Photo • Next rotation in " + durationStr);
                    }
                    else
                    {
                        _mainWindow.UpdateStatus("Slideshow: Active • Fixed interval: " + durationStr);
                    }
                }
            }
        }

        private void TogglePause()
        {
            _isPaused = !_isPaused;
            if (_isPaused)
            {
                _slideshowTimer.Stop();
                _menuPauseItem.Text = "▶️ Resume Slideshow";
                ShowTrayNotification("Slideshow paused.");
                if (_mainWindow != null) _mainWindow.UpdateStatus("Slideshow: Paused.");
            }
            else
            {
                if (_config.EnableSlideshow) _slideshowTimer.Start();
                _menuPauseItem.Text = "⏸️ Pause Slideshow";
                ShowTrayNotification("Slideshow resumed.");
                if (_mainWindow != null) _mainWindow.UpdateStatus("Slideshow: Active.");
            }
        }

        private void OnSlideshowTick(object sender, EventArgs e)
        {
            if (_isPaused) return;

            // Pause if user is playing fullscreen game
            if (_config.PauseWhileGaming && FullscreenDetector.IsFullscreenAppRunning())
            {
                Log("OnSlideshowTick: Fullscreen app/game detected — Slideshow postponed.");
                if (_mainWindow != null) _mainWindow.UpdateStatus("Fullscreen game/app detected — Slideshow postponed.");
                return;
            }

            Log("OnSlideshowTick: Triggering next wallpapers (animate=true)");
            TriggerNextWallpapers(true);

            if (_config.LockScreen.Enabled && _config.LockScreen.RandomizeOnTimer)
            {
                TriggerNextLockScreen();
            }

            // Immediately choose a new random interval for the upcoming cycle!
            if (_config.RandomInterval)
            {
                UpdateTimerInterval();
            }
        }

        public void TriggerNextWallpapers(bool animate)
        {
            var monitors = WallpaperManager.GetAllMonitors();
            for (int i = 0; i < monitors.Count; i++)
            {
                TriggerNextWallpaperForMonitor(i, animate);
            }
        }

        public void TriggerNextWallpaperForMonitor(int monitorIndex, bool animate = true)
        {
            var monitors = WallpaperManager.GetAllMonitors();
            if (monitorIndex < 0 || monitorIndex >= monitors.Count) return;

            var monInfo = monitors[monitorIndex];

            // Ensure config has monitor entry
            while (_config.Monitors.Count <= monitorIndex)
            {
                _config.Monitors.Add(new MonitorConfig
                {
                    DevicePath = monInfo.DevicePath,
                    FriendlyName = monInfo.DisplayName
                });
            }

            var monCfg = _config.Monitors[monitorIndex];
            if (monCfg.ImagePaths == null || monCfg.ImagePaths.Count == 0)
            {
                if (_mainWindow != null)
                    _mainWindow.UpdateStatus(monInfo.DisplayName + ": No images configured.");
                return;
            }

            // Pick next image
            string nextImage = "";
            bool isRandom = _config.RandomPhoto || monCfg.Mode == "Random";

            if (!isRandom)
            {
                monCfg.CurrentIndex = (monCfg.CurrentIndex + 1) % monCfg.ImagePaths.Count;
                nextImage = monCfg.ImagePaths[monCfg.CurrentIndex];
            }
            else
            {
                // Truly random without immediate repeat
                if (monCfg.ImagePaths.Count > 1)
                {
                    var candidates = monCfg.ImagePaths.FindAll(p => !string.IsNullOrEmpty(p) && p != monCfg.LastAppliedImage && File.Exists(p));
                    if (candidates.Count > 0)
                    {
                        nextImage = candidates[_rng.Next(candidates.Count)];
                    }
                    else
                    {
                        nextImage = monCfg.ImagePaths[_rng.Next(monCfg.ImagePaths.Count)];
                    }
                }
                else
                {
                    nextImage = monCfg.ImagePaths[0];
                }
            }

            if (!File.Exists(nextImage)) return;

            string oldImage = monCfg.LastAppliedImage;
            if (string.IsNullOrEmpty(oldImage) || !File.Exists(oldImage))
            {
                oldImage = WallpaperManager.GetCurrentWallpaper(monInfo.DevicePath);
            }
            monCfg.LastAppliedImage = nextImage;
            _config.Save();

            string nextInfo = (_config.RandomInterval && _slideshowTimer != null)
                ? " • Next: " + FormatDuration((int)_slideshowTimer.Interval.TotalSeconds)
                : "";

            bool isCovered = _config.DisableAnimationWhenCovered && FullscreenDetector.IsScreenCovered(monInfo.Bounds);
            bool runAnimation = animate && _config.EnableTransitions && !isCovered;

            Log(string.Format("TriggerNextWallpaper: mon={0}, img={1}, oldImg={2}, animate={3}, enableTrans={4}, disableWhenCovered={5}, isCovered={6}, runAnim={7}",
                monitorIndex, Path.GetFileName(nextImage), Path.GetFileName(oldImage), animate, _config.EnableTransitions, _config.DisableAnimationWhenCovered, isCovered, runAnimation));

            if (runAnimation)
            {
                TransitionEngine.ExecuteTransition(
                    monInfo.Bounds,
                    monInfo.DevicePath,
                    oldImage,
                    nextImage,
                    _config.TransitionType,
                    _config.TransitionDurationMs,
                    _config.EasingType,
                    () =>
                    {
                        if (_mainWindow != null)
                            _mainWindow.UpdateStatus("Changed: " + Path.GetFileName(nextImage) + " on " + monInfo.DisplayName + nextInfo);
                    });
            }
            else
            {
                WallpaperManager.SetWallpaper(monInfo.DevicePath, nextImage);
                if (_mainWindow != null)
                {
                    string status = "Changed: " + Path.GetFileName(nextImage) + " on " + monInfo.DisplayName + nextInfo;
                    if (isCovered) status += " [0% GPU - Screen Covered]";
                    _mainWindow.UpdateStatus(status);
                }
            }
        }

        public void TriggerTestTransition(int monitorIndex)
        {
            var monitors = WallpaperManager.GetAllMonitors();
            if (monitorIndex < 0 || monitorIndex >= monitors.Count)
                monitorIndex = 0;

            var monInfo = monitors[monitorIndex];
            var monCfg = monitorIndex < _config.Monitors.Count ? _config.Monitors[monitorIndex] : null;

            string targetImg = "";
            if (monCfg != null && monCfg.ImagePaths.Count > 0)
            {
                targetImg = monCfg.ImagePaths[_rng.Next(monCfg.ImagePaths.Count)];
            }
            else if (_config.LockScreen.ImagePaths.Count > 0)
            {
                targetImg = _config.LockScreen.ImagePaths[_rng.Next(_config.LockScreen.ImagePaths.Count)];
            }

            if (string.IsNullOrEmpty(targetImg) || !File.Exists(targetImg))
            {
                System.Windows.MessageBox.Show("Add at least one image to the monitor list to test the transition!", "Transition Test", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            TransitionEngine.ExecuteTransition(
                monInfo.Bounds,
                monInfo.DevicePath,
                monInfo.CurrentWallpaper,
                targetImg,
                _config.TransitionType,
                _config.TransitionDurationMs,
                _config.EasingType,
                null);
        }

        public void TriggerNextLockScreen()
        {
            if (_config.LockScreen.ImagePaths == null || _config.LockScreen.ImagePaths.Count == 0)
            {
                if (_mainWindow != null)
                    _mainWindow.UpdateStatus("Lock Screen: No images configured.");
                return;
            }

            string nextImage = "";
            if (_config.LockScreen.ImagePaths.Count > 1)
            {
                var candidates = _config.LockScreen.ImagePaths.FindAll(p => !string.IsNullOrEmpty(p) && p != _config.LockScreen.LastAppliedImage && File.Exists(p));
                if (candidates.Count > 0)
                {
                    nextImage = candidates[_rng.Next(candidates.Count)];
                }
                else
                {
                    nextImage = _config.LockScreen.ImagePaths[_rng.Next(_config.LockScreen.ImagePaths.Count)];
                }
            }
            else
            {
                nextImage = _config.LockScreen.ImagePaths[0];
            }

            if (!File.Exists(nextImage)) return;

            _config.LockScreen.LastAppliedImage = nextImage;
            _config.Save();

            bool ok = LockScreenManager.SetLockScreen(nextImage);
            if (_mainWindow != null)
            {
                if (ok)
                {
                    _mainWindow.UpdateStatus("Lock Screen updated successfully: " + Path.GetFileName(nextImage));
                }
                else
                {
                    _mainWindow.UpdateStatus("Lock Screen: Could not update (check permissions).");
                }
            }
        }
    }
}
