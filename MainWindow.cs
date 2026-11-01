using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MultiWallFX
{
    public class ImageListItem
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public ImageSource Thumbnail { get; set; }
    }

    public class MainWindow : Window
    {
        private AppConfig _config;
        private List<MonitorInfo> _monitors;
        private int _selectedMonitorIndex = 0;

        // Custom Navigation Tabs
        private Border _tabMonitorsBtn;
        private Border _tabLockBtn;
        private Border _tabTransBtn;
        private Border _tabSettingsBtn;
        private TextBlock _tabMonitorsTxt;
        private TextBlock _tabLockTxt;
        private TextBlock _tabTransTxt;
        private TextBlock _tabSettingsTxt;
        private Grid _contentContainer;
        private UIElement _viewMonitors;
        private UIElement _viewLock;
        private UIElement _viewTrans;
        private UIElement _viewSettings;
        private int _currentTabIndex = 0;

        // UI Controls - Monitors Tab
        private System.Windows.Controls.ComboBox _cbMonitors;
        private System.Windows.Controls.ComboBox _cbMonitorMode;
        private System.Windows.Controls.ListBox _lbMonitorImages;
        private StackPanel _emptyMonitorPlaceholder;
        private TextBlock _tbMonitorCount;

        // UI Controls - Lock Screen Tab
        private System.Windows.Controls.CheckBox _chkLockEnabled;
        private System.Windows.Controls.CheckBox _chkLockStartup;
        private System.Windows.Controls.CheckBox _chkLockTimer;
        private System.Windows.Controls.ListBox _lbLockImages;
        private StackPanel _emptyLockPlaceholder;
        private TextBlock _tbLockCount;

        // UI Controls - Transitions Tab
        private System.Windows.Controls.CheckBox _chkEnableTransitions;
        private System.Windows.Controls.CheckBox _chkSkipWhenCovered;
        private System.Windows.Controls.ComboBox _cbTransitionType;
        private Slider _sliderDuration;
        private TextBlock _tbDurationValue;
        private System.Windows.Controls.ComboBox _cbEasingType;

        // UI Controls - Settings Tab
        private System.Windows.Controls.CheckBox _chkStartWithWindows;
        private System.Windows.Controls.CheckBox _chkRandomStartup;
        private System.Windows.Controls.CheckBox _chkEnableSlideshow;
        private System.Windows.Controls.ComboBox _cbInterval;
        private StackPanel _intStack;
        private System.Windows.Controls.CheckBox _chkRandomInterval;
        private System.Windows.Controls.ComboBox _cbRandomMin;
        private System.Windows.Controls.ComboBox _cbRandomMax;
        private System.Windows.Controls.CheckBox _chkRandomPhoto;
        private StackPanel _panelRandomRange;
        private System.Windows.Controls.CheckBox _chkPauseGaming;
        private TextBlock _tbStatus;

        public MainWindow(AppConfig config)
        {
            _config = config;
            InitializeComponent();
            LoadMonitorsAndData();
            SwitchTab(0);

            Loaded += (s, e) =>
            {
                if (!_config.HasSeenWelcome)
                {
                    _config.HasSeenWelcome = true;
                    _config.Save();
                    ShowWelcomeDialog();
                }
            };
        }

        private void InitializeComponent()
        {
            Title = "MultiWall FX — Wallpaper & Lock Screen Manager";
            Width = 920;
            Height = 680;
            MinWidth = 850;
            MinHeight = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 17, 26)); // #0f111a
            Foreground = System.Windows.Media.Brushes.White;
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI, Segoe UI Variable Display, Arial");

            try
            {
                string icoFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(icoFile))
                {
                    Icon = BitmapFrame.Create(new Uri(icoFile));
                }
            }
            catch { }

            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(65) });  // Header
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(56) });  // Custom Nav Tabs
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // View Area
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) });  // Status Footer

            // ================= 1. HEADER =================
            var headerBorder = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 24, 38)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(38, 42, 65)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(24, 0, 24, 0)
            };
            var headerDock = new DockPanel { VerticalAlignment = VerticalAlignment.Center };

            // Right: Change All Now Button
            var btnChangeAll = CreateActionButton("⚡ Change All Now", "#6366f1", 180, 36);
            btnChangeAll.Click += (s, e) => App.Instance.TriggerNextWallpapers(true);
            DockPanel.SetDock(btnChangeAll, Dock.Right);
            headerDock.Children.Add(btnChangeAll);

            // Left: Title & Badge
            var titleStack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var iconBox = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(99, 102, 241)),
                CornerRadius = new CornerRadius(8),
                Width = 36,
                Height = 36,
                Margin = new Thickness(0, 0, 12, 0)
            };
            var iconTxt = new TextBlock
            {
                Text = "🎨",
                FontSize = 18,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBox.Child = iconTxt;
            titleStack.Children.Add(iconBox);

            var titleInfoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var titleText = new TextBlock
            {
                Text = "MultiWall FX",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White
            };
            var subtitleText = new TextBlock
            {
                Text = "Per-Monitor Slideshow • Lock Screen • Hardware-Accelerated Transitions",
                FontSize = 11,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184))
            };
            titleInfoStack.Children.Add(titleText);
            titleInfoStack.Children.Add(subtitleText);
            titleStack.Children.Add(titleInfoStack);

            headerDock.Children.Add(titleStack);
            headerBorder.Child = headerDock;
            Grid.SetRow(headerBorder, 0);
            rootGrid.Children.Add(headerBorder);

            // ================= 2. CUSTOM NAV BAR =================
            var navBorder = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(18, 20, 32)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(35, 38, 58)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(20, 8, 20, 8)
            };
            var navStack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            _tabMonitorsBtn = CreateTabButton("🖥️  Monitors (Wallpapers)", 0, out _tabMonitorsTxt);
            _tabLockBtn = CreateTabButton("🔒  Lock Screen", 1, out _tabLockTxt);
            _tabTransBtn = CreateTabButton("✨  Transitions & FX", 2, out _tabTransTxt);
            _tabSettingsBtn = CreateTabButton("⚙️  Settings & Automation", 3, out _tabSettingsTxt);

            navStack.Children.Add(_tabMonitorsBtn);
            navStack.Children.Add(_tabLockBtn);
            navStack.Children.Add(_tabTransBtn);
            navStack.Children.Add(_tabSettingsBtn);
            navBorder.Child = navStack;
            Grid.SetRow(navBorder, 1);
            rootGrid.Children.Add(navBorder);

            // ================= 3. CONTENT AREA =================
            _contentContainer = new Grid { Margin = new Thickness(20, 15, 20, 10) };

            _viewMonitors = CreateMonitorsView();
            _viewLock = CreateLockScreenView();
            _viewTrans = CreateTransitionsView();
            _viewSettings = CreateSettingsView();

            _contentContainer.Children.Add(_viewMonitors);
            _contentContainer.Children.Add(_viewLock);
            _contentContainer.Children.Add(_viewTrans);
            _contentContainer.Children.Add(_viewSettings);

            Grid.SetRow(_contentContainer, 2);
            rootGrid.Children.Add(_contentContainer);

            // ================= 4. STATUS BAR =================
            var statusBorder = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(12, 14, 22)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(28, 30, 48)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(24, 0, 24, 0)
            };
            var statusDock = new DockPanel { VerticalAlignment = VerticalAlignment.Center };

            var rightStack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var authorBadge = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(45, 99, 102, 241)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(140, 129, 140, 248)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 3, 10, 3),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "Click to open v1te0k's GitHub Profile (https://github.com/v1te0k)"
            };

            var badgeStack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            badgeStack.Children.Add(new TextBlock
            {
                Text = "🐙 Made by ",
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240)),
                FontSize = 11,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center
            });
            badgeStack.Children.Add(new TextBlock
            {
                Text = "v1te0k",
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(165, 180, 252)),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                TextDecorations = TextDecorations.Underline,
                VerticalAlignment = VerticalAlignment.Center
            });
            badgeStack.Children.Add(new TextBlock
            {
                Text = " ↗",
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(165, 180, 252)),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 0, 0)
            });
            authorBadge.Child = badgeStack;

            authorBadge.MouseEnter += (s, e) =>
            {
                authorBadge.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(90, 99, 102, 241));
                authorBadge.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(165, 180, 252));
            };
            authorBadge.MouseLeave += (s, e) =>
            {
                authorBadge.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(45, 99, 102, 241));
                authorBadge.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(140, 129, 140, 248));
            };
            authorBadge.MouseDown += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start("https://github.com/v1te0k");
                }
                catch { }
            };

            var fpsTxt = new TextBlock
            {
                Text = "• DirectX 60 FPS",
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184)),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };

            rightStack.Children.Add(authorBadge);
            rightStack.Children.Add(fpsTxt);
            DockPanel.SetDock(rightStack, Dock.Right);
            statusDock.Children.Add(rightStack);

            _tbStatus = new TextBlock
            {
                Text = "● Ready to use. Add images to get started.",
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 222, 128)), // Bright green
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            statusDock.Children.Add(_tbStatus);

            statusBorder.Child = statusDock;
            Grid.SetRow(statusBorder, 3);
            rootGrid.Children.Add(statusBorder);

            Content = rootGrid;

            Closing += (s, e) =>
            {
                e.Cancel = true;
                Hide();
                _config.Save();
                App.Instance.ShowTrayNotification("MultiWall FX is running in the background in System Tray.");
            };
        }

        // ================= TAB BUTTON HELPER =================
        private Border CreateTabButton(string text, int tabIndex, out TextBlock txtBlock)
        {
            var border = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 8, 16, 8),
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(28, 31, 48))
            };

            txtBlock = new TextBlock
            {
                Text = text,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225)) // #cbd5e1
            };
            border.Child = txtBlock;

            int idx = tabIndex;
            border.MouseDown += (s, e) => SwitchTab(idx);

            border.MouseEnter += (s, e) =>
            {
                if (_currentTabIndex != idx)
                {
                    border.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 44, 68));
                }
            };
            border.MouseLeave += (s, e) =>
            {
                if (_currentTabIndex != idx)
                {
                    border.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(28, 31, 48));
                }
            };

            return border;
        }

        private void SwitchTab(int index)
        {
            _currentTabIndex = index;

            // Update Views Visibility
            _viewMonitors.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
            _viewLock.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
            _viewTrans.Visibility = index == 2 ? Visibility.Visible : Visibility.Collapsed;
            _viewSettings.Visibility = index == 3 ? Visibility.Visible : Visibility.Collapsed;

            // Highlight Active Button
            SetTabButtonStyle(_tabMonitorsBtn, _tabMonitorsTxt, index == 0);
            SetTabButtonStyle(_tabLockBtn, _tabLockTxt, index == 1);
            SetTabButtonStyle(_tabTransBtn, _tabTransTxt, index == 2);
            SetTabButtonStyle(_tabSettingsBtn, _tabSettingsTxt, index == 3);
        }

        private void SetTabButtonStyle(Border btn, TextBlock txt, bool isActive)
        {
            if (isActive)
            {
                btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(79, 70, 229)); // Vibrant Indigo #4f46e5
                btn.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(129, 140, 248));
                btn.BorderThickness = new Thickness(1);
                txt.Foreground = System.Windows.Media.Brushes.White;
                txt.FontWeight = FontWeights.Bold;
            }
            else
            {
                btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 29, 45));
                btn.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(38, 42, 65));
                btn.BorderThickness = new Thickness(1);
                txt.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225));
                txt.FontWeight = FontWeights.SemiBold;
            }
        }

        // ================= TAB 1: MONITORS VIEW =================
        private UIElement CreateMonitorsView()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Top Bar: Card for Monitor selection and mode
            var topCard = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 25, 38)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 44, 68)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 12)
            };
            var topStack = new DockPanel();

            var leftControls = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            leftControls.Children.Add(new TextBlock
            {
                Text = "Active Display:",
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            });

            _cbMonitors = CreateStyledComboBox(320);
            _cbMonitors.SelectionChanged += (s, e) => OnMonitorChanged();
            leftControls.Children.Add(_cbMonitors);

            leftControls.Children.Add(new TextBlock
            {
                Text = "Rotation Mode:",
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(24, 0, 10, 0)
            });

            _cbMonitorMode = CreateStyledComboBox(170);
            _cbMonitorMode.Items.Add("🎲 Random (Shuffle)");
            _cbMonitorMode.Items.Add("➡️ Sequential (List Order)");
            _cbMonitorMode.SelectedIndex = 0;
            _cbMonitorMode.SelectionChanged += (s, e) =>
            {
                var curMon = GetCurrentMonitorConfig();
                if (curMon != null)
                {
                    curMon.Mode = _cbMonitorMode.SelectedIndex == 0 ? "Random" : "Sequential";
                    _config.Save();
                }
            };
            leftControls.Children.Add(_cbMonitorMode);

            topStack.Children.Add(leftControls);
            topCard.Child = topStack;
            Grid.SetRow(topCard, 0);
            grid.Children.Add(topCard);

            // Middle: Images List & Empty state overlay
            var listContainer = new Grid();

            var listBorder = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 22, 34)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 44, 68)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8)
            };
            _lbMonitorImages = new System.Windows.Controls.ListBox
            {
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                SelectionMode = System.Windows.Controls.SelectionMode.Extended,
                ItemTemplate = CreateImageItemDataTemplate()
            };
            listBorder.Child = _lbMonitorImages;
            listContainer.Children.Add(listBorder);

            // Empty placeholder
            _emptyMonitorPlaceholder = CreateEmptyPlaceholder("No images selected for this monitor", "Click [+ Add Images] or [+ Add Folder] below to start.");
            listContainer.Children.Add(_emptyMonitorPlaceholder);

            Grid.SetRow(listContainer, 1);
            grid.Children.Add(listContainer);

            // Bottom Actions Bar
            var bottomDock = new DockPanel { Margin = new Thickness(0, 12, 0, 0) };

            _tbMonitorCount = new TextBlock
            {
                Text = "0 images in list",
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184)),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(_tbMonitorCount, Dock.Right);
            bottomDock.Children.Add(_tbMonitorCount);

            var buttonsStack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };

            var btnAddFiles = CreateActionButton("+ Add Images", "#2563eb", 130, 34);
            btnAddFiles.Click += (s, e) => AddImagesToCurrentMonitor();
            buttonsStack.Children.Add(btnAddFiles);

            var btnAddFolder = CreateActionButton("+ Add Folder", "#0284c7", 130, 34);
            btnAddFolder.Click += (s, e) => AddFolderToCurrentMonitor();
            buttonsStack.Children.Add(btnAddFolder);

            var btnDelete = CreateActionButton("🗑️ Remove", "#dc2626", 90, 34);
            btnDelete.Click += (s, e) => RemoveSelectedImagesFromCurrentMonitor();
            buttonsStack.Children.Add(btnDelete);

            var btnClear = CreateActionButton("🧹 Clear All", "#475569", 100, 34);
            btnClear.Click += (s, e) => ClearImagesFromCurrentMonitor();
            buttonsStack.Children.Add(btnClear);

            var btnApplyNow = CreateActionButton("▶ Change This Display Now", "#16a34a", 210, 34);
            btnApplyNow.Click += (s, e) => App.Instance.TriggerNextWallpaperForMonitor(_selectedMonitorIndex);
            buttonsStack.Children.Add(btnApplyNow);

            bottomDock.Children.Add(buttonsStack);
            Grid.SetRow(bottomDock, 2);
            grid.Children.Add(bottomDock);

            return grid;
        }

        // ================= TAB 2: LOCK SCREEN VIEW =================
        private UIElement CreateLockScreenView()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Top Options Card
            var topCard = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 25, 38)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 44, 68)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 12)
            };
            var optStack = new StackPanel();

            _chkLockEnabled = CreateStyledCheckBox("Enable Windows Lock Screen randomization", 14, true);
            _chkLockEnabled.IsChecked = _config.LockScreen.Enabled;
            _chkLockEnabled.Checked += (s, e) => { _config.LockScreen.Enabled = true; _config.Save(); };
            _chkLockEnabled.Unchecked += (s, e) => { _config.LockScreen.Enabled = false; _config.Save(); };
            optStack.Children.Add(_chkLockEnabled);

            var subChecks = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(26, 8, 0, 0) };

            _chkLockStartup = CreateStyledCheckBox("Change automatically when PC starts (Startup)", 13, false);
            _chkLockStartup.IsChecked = _config.LockScreen.RandomizeOnStartup;
            _chkLockStartup.Checked += (s, e) => { _config.LockScreen.RandomizeOnStartup = true; _config.Save(); };
            _chkLockStartup.Unchecked += (s, e) => { _config.LockScreen.RandomizeOnStartup = false; _config.Save(); };
            subChecks.Children.Add(_chkLockStartup);

            _chkLockTimer = CreateStyledCheckBox("Change on timer interval as well (Slideshow)", 13, false);
            _chkLockTimer.Margin = new Thickness(30, 0, 0, 0);
            _chkLockTimer.IsChecked = _config.LockScreen.RandomizeOnTimer;
            _chkLockTimer.Checked += (s, e) => { _config.LockScreen.RandomizeOnTimer = true; _config.Save(); };
            _chkLockTimer.Unchecked += (s, e) => { _config.LockScreen.RandomizeOnTimer = false; _config.Save(); };
            subChecks.Children.Add(_chkLockTimer);

            optStack.Children.Add(subChecks);
            topCard.Child = optStack;
            Grid.SetRow(topCard, 0);
            grid.Children.Add(topCard);

            // Images List
            var listContainer = new Grid();

            var listBorder = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 22, 34)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 44, 68)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8)
            };
            _lbLockImages = new System.Windows.Controls.ListBox
            {
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                SelectionMode = System.Windows.Controls.SelectionMode.Extended,
                ItemTemplate = CreateImageItemDataTemplate()
            };
            listBorder.Child = _lbLockImages;
            listContainer.Children.Add(listBorder);

            _emptyLockPlaceholder = CreateEmptyPlaceholder("No images configured for Lock Screen", "Add portrait or widescreen images for your Windows lock screen.");
            listContainer.Children.Add(_emptyLockPlaceholder);

            Grid.SetRow(listContainer, 1);
            grid.Children.Add(listContainer);

            // Bottom Buttons
            var bottomDock = new DockPanel { Margin = new Thickness(0, 12, 0, 0) };

            _tbLockCount = new TextBlock
            {
                Text = "0 images for Lock Screen",
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184)),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(_tbLockCount, Dock.Right);
            bottomDock.Children.Add(_tbLockCount);

            var buttonsStack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };

            var btnAddFiles = CreateActionButton("+ Add Images", "#2563eb", 130, 34);
            btnAddFiles.Click += (s, e) => AddImagesToLockScreen();
            buttonsStack.Children.Add(btnAddFiles);

            var btnAddFolder = CreateActionButton("+ Add Folder", "#0284c7", 130, 34);
            btnAddFolder.Click += (s, e) => AddFolderToLockScreen();
            buttonsStack.Children.Add(btnAddFolder);

            var btnDelete = CreateActionButton("🗑️ Remove", "#dc2626", 90, 34);
            btnDelete.Click += (s, e) => RemoveSelectedImagesFromLockScreen();
            buttonsStack.Children.Add(btnDelete);

            var btnApplyLock = CreateActionButton("🔒 Set Lock Screen Now", "#7c3aed", 200, 34);
            btnApplyLock.Click += (s, e) => App.Instance.TriggerNextLockScreen();
            buttonsStack.Children.Add(btnApplyLock);

            bottomDock.Children.Add(buttonsStack);
            Grid.SetRow(bottomDock, 2);
            grid.Children.Add(bottomDock);

            return grid;
        }

        // ================= TAB 3: TRANSITIONS & FX VIEW =================
        private UIElement CreateTransitionsView()
        {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel { Margin = new Thickness(0, 5, 0, 15) };

            _chkEnableTransitions = CreateStyledCheckBox("Enable Hardware-Accelerated Transitions (DirectX 60 FPS)", 15, true);
            _chkEnableTransitions.IsChecked = _config.EnableTransitions;
            _chkEnableTransitions.Checked += (s, e) => { _config.EnableTransitions = true; _config.Save(); };
            _chkEnableTransitions.Unchecked += (s, e) => { _config.EnableTransitions = false; _config.Save(); };
            stack.Children.Add(_chkEnableTransitions);

            _chkSkipWhenCovered = CreateStyledCheckBox("⚡ Performance Optimization: Skip animation when screen is covered (Maximized/Fullscreen app)", 13, false);
            _chkSkipWhenCovered.Margin = new Thickness(26, 6, 0, 12);
            _chkSkipWhenCovered.IsChecked = _config.DisableAnimationWhenCovered;
            _chkSkipWhenCovered.Checked += (s, e) => { _config.DisableAnimationWhenCovered = true; _config.Save(); };
            _chkSkipWhenCovered.Unchecked += (s, e) => { _config.DisableAnimationWhenCovered = false; _config.Save(); };
            stack.Children.Add(_chkSkipWhenCovered);

            // Card 1: Preset Transition Selection
            var typeStack = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };

            _cbTransitionType = CreateStyledComboBox(460);
            _cbTransitionType.Items.Add(new ComboBoxItem { Content = "🌊 Fade Out / Cross-Fade (Smooth classic blend)", Tag = "Fade" });
            _cbTransitionType.Items.Add(new ComboBoxItem { Content = "⬅️ Slide Left (Right -> Left slide)", Tag = "SlideLeft" });
            _cbTransitionType.Items.Add(new ComboBoxItem { Content = "➡️ Slide Right (Left -> Right slide)", Tag = "SlideRight" });
            _cbTransitionType.Items.Add(new ComboBoxItem { Content = "⬆️ Slide Up (Bottom -> Top slide)", Tag = "SlideUp" });
            _cbTransitionType.Items.Add(new ComboBoxItem { Content = "⬇️ Slide Down (Top -> Bottom slide)", Tag = "SlideDown" });
            _cbTransitionType.Items.Add(new ComboBoxItem { Content = "🔍 Zoom & Fade (Cinematic Ken Burns)", Tag = "Zoom" });
            _cbTransitionType.Items.Add(new ComboBoxItem { Content = "🎲 Random Effect (Pick a different style every time)", Tag = "Random" });
            _cbTransitionType.Items.Add(new ComboBoxItem { Content = "⚡ Instant Switch (No transition)", Tag = "None" });

            SelectComboItemByTag(_cbTransitionType, _config.TransitionType);
            _cbTransitionType.SelectionChanged += (s, e) =>
            {
                var item = _cbTransitionType.SelectedItem as ComboBoxItem;
                if (item != null)
                {
                    _config.TransitionType = item.Tag.ToString();
                    _config.Save();
                }
            };
            typeStack.Children.Add(_cbTransitionType);
            stack.Children.Add(CreateSettingCard("1. Visual Transition Style", "Choose how the current wallpaper transitions into the next one.", typeStack));

            // Card 2: Custom Speed & Easing Engine
            var customStack = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };

            // Duration Slider
            var durHeader = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
            _tbDurationValue = new TextBlock
            {
                Text = _config.TransitionDurationMs + " ms (" + (_config.TransitionDurationMs / 1000.0).ToString("0.1") + " seconds)",
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(129, 140, 248)),
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };
            DockPanel.SetDock(_tbDurationValue, Dock.Right);
            durHeader.Children.Add(_tbDurationValue);

            durHeader.Children.Add(new TextBlock
            {
                Text = "Transition Duration:",
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13
            });
            customStack.Children.Add(durHeader);

            _sliderDuration = new Slider
            {
                Minimum = 300,
                Maximum = 4000,
                TickFrequency = 100,
                IsSnapToTickEnabled = true,
                Value = _config.TransitionDurationMs,
                Margin = new Thickness(0, 0, 0, 18)
            };
            _sliderDuration.ValueChanged += (s, e) =>
            {
                int val = (int)_sliderDuration.Value;
                _config.TransitionDurationMs = val;
                _tbDurationValue.Text = val + " ms (" + (val / 1000.0).ToString("0.1") + " seconds)";
                _config.Save();
            };
            customStack.Children.Add(_sliderDuration);

            // Easing Curve
            customStack.Children.Add(new TextBlock
            {
                Text = "Motion / Acceleration Curve (Easing):",
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 8)
            });

            _cbEasingType = CreateStyledComboBox(420);
            _cbEasingType.Items.Add(new ComboBoxItem { Content = "Cubic (Very smooth and natural — Recommended)", Tag = "Cubic" });
            _cbEasingType.Items.Add(new ComboBoxItem { Content = "Quadratic (Fast start, gentle deceleration)", Tag = "Quadratic" });
            _cbEasingType.Items.Add(new ComboBoxItem { Content = "Sine (Gentle uniform wave)", Tag = "Sine" });
            _cbEasingType.Items.Add(new ComboBoxItem { Content = "Bounce (Playful elastic effect)", Tag = "Bounce" });
            _cbEasingType.Items.Add(new ComboBoxItem { Content = "Linear (Constant uniform speed)", Tag = "Linear" });

            SelectComboItemByTag(_cbEasingType, _config.EasingType);
            _cbEasingType.SelectionChanged += (s, e) =>
            {
                var item = _cbEasingType.SelectedItem as ComboBoxItem;
                if (item != null)
                {
                    _config.EasingType = item.Tag.ToString();
                    _config.Save();
                }
            };
            customStack.Children.Add(_cbEasingType);

            // Test button
            var testPanel = new StackPanel { Margin = new Thickness(0, 20, 0, 0) };
            var btnTestTransition = CreateActionButton("▶ Test Transition Live on Screen", "#7c3aed", 300, 38);
            btnTestTransition.Click += (s, e) => App.Instance.TriggerTestTransition(_selectedMonitorIndex);
            testPanel.Children.Add(btnTestTransition);

            customStack.Children.Add(testPanel);
            stack.Children.Add(CreateSettingCard("2. Motion Customization Engine (Speed & Easing Curve)", "Set the exact duration in milliseconds and your preferred acceleration curve.", customStack));

            scroll.Content = stack;
            return scroll;
        }

        // ================= TAB 4: SETTINGS VIEW =================
        private UIElement CreateSettingsView()
        {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel { Margin = new Thickness(0, 5, 0, 15) };

            // Startup Card
            var startStack = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };

            _chkStartWithWindows = CreateStyledCheckBox("Start with Windows (Minimized to System Tray)", 14, true);
            _chkStartWithWindows.IsChecked = _config.StartWithWindows;
            _chkStartWithWindows.Checked += (s, e) =>
            {
                _config.StartWithWindows = true;
                StartupHelper.SetStartup(true);
                _config.Save();
            };
            _chkStartWithWindows.Unchecked += (s, e) =>
            {
                _config.StartWithWindows = false;
                StartupHelper.SetStartup(false);
                _config.Save();
            };
            startStack.Children.Add(_chkStartWithWindows);

            _chkRandomStartup = CreateStyledCheckBox("Pick a random wallpaper every time Windows boots", 13, false);
            _chkRandomStartup.Margin = new Thickness(26, 8, 0, 0);
            _chkRandomStartup.IsChecked = _config.RandomizeOnStartup;
            _chkRandomStartup.Checked += (s, e) => { _config.RandomizeOnStartup = true; _config.Save(); };
            _chkRandomStartup.Unchecked += (s, e) => { _config.RandomizeOnStartup = false; _config.Save(); };
            startStack.Children.Add(_chkRandomStartup);

            stack.Children.Add(CreateSettingCard("Windows Startup Automation", "The application launches automatically and quietly in the background.", startStack));

            // Random Time & Photo Card
            var randStack = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };

            _chkRandomInterval = CreateStyledCheckBox("🎲 Enable Random Time between changes (Random Interval)", 14, true);
            _chkRandomInterval.IsChecked = _config.RandomInterval;
            randStack.Children.Add(_chkRandomInterval);

            var randTimeDesc = new TextBlock
            {
                Text = "Each change will wait an unpredictable duration between the minimum and maximum selected below.",
                FontSize = 11,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184)),
                Margin = new Thickness(26, 4, 0, 8)
            };
            randStack.Children.Add(randTimeDesc);

            _panelRandomRange = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(26, 2, 0, 12) };

            _panelRandomRange.Children.Add(new TextBlock
            {
                Text = "Min Time:",
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            });

            _cbRandomMin = CreateStyledComboBox(130);
            PopulateIntervalCombo(_cbRandomMin);
            SelectComboItemByIntTag(_cbRandomMin, _config.RandomIntervalMinSeconds);
            _panelRandomRange.Children.Add(_cbRandomMin);

            _panelRandomRange.Children.Add(new TextBlock
            {
                Text = "Max Time:",
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(20, 0, 8, 0)
            });

            _cbRandomMax = CreateStyledComboBox(130);
            PopulateIntervalCombo(_cbRandomMax);
            SelectComboItemByIntTag(_cbRandomMax, _config.RandomIntervalMaxSeconds);
            _panelRandomRange.Children.Add(_cbRandomMax);

            randStack.Children.Add(_panelRandomRange);

            _chkRandomInterval.Checked += (s, e) =>
            {
                _config.RandomInterval = true;
                _panelRandomRange.Opacity = 1.0;
                _panelRandomRange.IsEnabled = true;
                if (_intStack != null)
                {
                    _intStack.Opacity = 0.35;
                    _cbInterval.IsEnabled = false;
                }
                App.Instance.UpdateTimerInterval();
                _config.Save();
            };
            _chkRandomInterval.Unchecked += (s, e) =>
            {
                _config.RandomInterval = false;
                _panelRandomRange.Opacity = 0.35;
                _panelRandomRange.IsEnabled = false;
                if (_intStack != null)
                {
                    _intStack.Opacity = 1.0;
                    _cbInterval.IsEnabled = true;
                }
                App.Instance.UpdateTimerInterval();
                _config.Save();
            };

            _cbRandomMin.SelectionChanged += (s, e) =>
            {
                var item = _cbRandomMin.SelectedItem as ComboBoxItem;
                if (item != null)
                {
                    int val = (int)item.Tag;
                    _config.RandomIntervalMinSeconds = val;
                    if (_config.RandomIntervalMaxSeconds <= val)
                    {
                        _config.RandomIntervalMaxSeconds = val + 30;
                        SelectComboItemByIntTag(_cbRandomMax, _config.RandomIntervalMaxSeconds);
                    }
                    App.Instance.UpdateTimerInterval();
                    _config.Save();
                }
            };

            _cbRandomMax.SelectionChanged += (s, e) =>
            {
                var item = _cbRandomMax.SelectedItem as ComboBoxItem;
                if (item != null)
                {
                    int val = (int)item.Tag;
                    _config.RandomIntervalMaxSeconds = val;
                    if (_config.RandomIntervalMinSeconds >= val)
                    {
                        _config.RandomIntervalMinSeconds = Math.Max(10, val - 30);
                        SelectComboItemByIntTag(_cbRandomMin, _config.RandomIntervalMinSeconds);
                    }
                    App.Instance.UpdateTimerInterval();
                    _config.Save();
                }
            };

            // Random Photo Checkbox
            _chkRandomPhoto = CreateStyledCheckBox("🖼️ 100% Random Photos (Shuffle without consecutive repeats)", 13, false);
            _chkRandomPhoto.Margin = new Thickness(0, 4, 0, 4);
            _chkRandomPhoto.IsChecked = _config.RandomPhoto;
            _chkRandomPhoto.Checked += (s, e) =>
            {
                _config.RandomPhoto = true;
                foreach (var m in _config.Monitors) m.Mode = "Random";
                _config.Save();
                if (_cbMonitorMode != null) _cbMonitorMode.SelectedIndex = 0;
            };
            _chkRandomPhoto.Unchecked += (s, e) =>
            {
                _config.RandomPhoto = false;
                _config.Save();
            };
            randStack.Children.Add(_chkRandomPhoto);

            var randPhotoDesc = new TextBlock
            {
                Text = "Guarantees each display gets a different random photo without repeating the same image consecutively.",
                FontSize = 11,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184)),
                Margin = new Thickness(26, 2, 0, 10)
            };
            randStack.Children.Add(randPhotoDesc);

            // Action button inside card
            var btnRandNow = CreateActionButton("⚡ Change Now (Random Time & Photo)", "#4f46e5", 280, 34);
            btnRandNow.Margin = new Thickness(26, 4, 0, 4);
            btnRandNow.Click += (s, e) =>
            {
                App.Instance.TriggerNextWallpapers(true);
                if (_config.RandomInterval) App.Instance.UpdateTimerInterval();
            };
            randStack.Children.Add(btnRandNow);

            stack.Children.Add(CreateSettingCard("🎲 Feature: Random Time & Photos (Shuffle FX)", "Control unpredictable timer intervals and randomized wallpaper selection.", randStack));

            // Slideshow Card
            var slideStack = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };

            _chkEnableSlideshow = CreateStyledCheckBox("Enable automatic periodic wallpaper slideshow", 14, true);
            _chkEnableSlideshow.IsChecked = _config.EnableSlideshow;
            _chkEnableSlideshow.Checked += (s, e) =>
            {
                _config.EnableSlideshow = true;
                App.Instance.UpdateTimerInterval();
                _config.Save();
            };
            _chkEnableSlideshow.Unchecked += (s, e) =>
            {
                _config.EnableSlideshow = false;
                App.Instance.UpdateTimerInterval();
                _config.Save();
            };
            slideStack.Children.Add(_chkEnableSlideshow);

            _intStack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(26, 12, 0, 10) };
            _intStack.Children.Add(new TextBlock
            {
                Text = "Alternative Fixed Interval:",
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            });

            _cbInterval = CreateStyledComboBox(220);
            _cbInterval.Items.Add(new ComboBoxItem { Content = "10 seconds (Quick Test)", Tag = 10 });
            _cbInterval.Items.Add(new ComboBoxItem { Content = "30 seconds", Tag = 30 });
            _cbInterval.Items.Add(new ComboBoxItem { Content = "1 minute", Tag = 60 });
            _cbInterval.Items.Add(new ComboBoxItem { Content = "5 minutes (Recommended)", Tag = 300 });
            _cbInterval.Items.Add(new ComboBoxItem { Content = "15 minutes", Tag = 900 });
            _cbInterval.Items.Add(new ComboBoxItem { Content = "30 minutes", Tag = 1800 });
            _cbInterval.Items.Add(new ComboBoxItem { Content = "1 hour", Tag = 3600 });
            _cbInterval.Items.Add(new ComboBoxItem { Content = "3 hours", Tag = 10800 });
            _cbInterval.Items.Add(new ComboBoxItem { Content = "6 hours", Tag = 21600 });
            _cbInterval.Items.Add(new ComboBoxItem { Content = "24 hours (Once a day)", Tag = 86400 });

            SelectComboItemByIntTag(_cbInterval, _config.IntervalSeconds);
            _cbInterval.SelectionChanged += (s, e) =>
            {
                var item = _cbInterval.SelectedItem as ComboBoxItem;
                if (item != null)
                {
                    _config.IntervalSeconds = (int)item.Tag;
                    App.Instance.UpdateTimerInterval();
                    _config.Save();
                }
            };
            _intStack.Children.Add(_cbInterval);
            slideStack.Children.Add(_intStack);

            // Sync visual states for Random vs Fixed interval
            if (_config.RandomInterval)
            {
                _panelRandomRange.Opacity = 1.0;
                _panelRandomRange.IsEnabled = true;
                _intStack.Opacity = 0.35;
                _cbInterval.IsEnabled = false;
            }
            else
            {
                _panelRandomRange.Opacity = 0.35;
                _panelRandomRange.IsEnabled = false;
                _intStack.Opacity = 1.0;
                _cbInterval.IsEnabled = true;
            }

            _chkPauseGaming = CreateStyledCheckBox("Pause automatically while full-screen games are running (0% GPU/CPU)", 13, false);
            _chkPauseGaming.Margin = new Thickness(26, 0, 0, 0);
            _chkPauseGaming.IsChecked = _config.PauseWhileGaming;
            _chkPauseGaming.Checked += (s, e) => { _config.PauseWhileGaming = true; _config.Save(); };
            _chkPauseGaming.Unchecked += (s, e) => { _config.PauseWhileGaming = false; _config.Save(); };
            slideStack.Children.Add(_chkPauseGaming);

            stack.Children.Add(CreateSettingCard("Automatic Slideshow While Working", "Rotates wallpapers in the background without disturbing your workflow.", slideStack));

            // Developer & About Card
            var devStack = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
            var devDock = new DockPanel();

            var btnGithubLink = CreateActionButton("🐙 Open GitHub Profile", "#1e293b", 190, 36);
            btnGithubLink.BorderThickness = new Thickness(1);
            btnGithubLink.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(71, 85, 105));
            btnGithubLink.Click += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start("https://github.com/v1te0k");
                }
                catch { }
            };
            DockPanel.SetDock(btnGithubLink, Dock.Right);
            devDock.Children.Add(btnGithubLink);

            var devTextStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var devName = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
            devName.Children.Add(new TextBlock
            {
                Text = "🐙 Created by ",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White
            });
            devName.Children.Add(new TextBlock
            {
                Text = "v1te0k",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(165, 180, 252))
            });
            var devUrl = new TextBlock
            {
                Text = "https://github.com/v1te0k ↗",
                FontSize = 12,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(96, 165, 250)),
                Cursor = System.Windows.Input.Cursors.Hand,
                TextDecorations = TextDecorations.Underline,
                Margin = new Thickness(0, 3, 0, 0)
            };
            devUrl.MouseDown += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start("https://github.com/v1te0k");
                }
                catch { }
            };
            devTextStack.Children.Add(devName);
            devTextStack.Children.Add(devUrl);
            devDock.Children.Add(devTextStack);
            devStack.Children.Add(devDock);

            stack.Children.Add(CreateSettingCard("Developer & Project Info", "Official profile and open-source projects.", devStack));

            // App Actions Card
            var actStack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };

            var btnSave = CreateActionButton("💾 Save All Settings", "#2563eb", 180, 36);
            btnSave.Click += (s, e) =>
            {
                _config.Save();
                System.Windows.MessageBox.Show("Your settings and lists have been saved successfully!", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            actStack.Children.Add(btnSave);

            var btnExit = CreateActionButton("❌ Exit Application", "#dc2626", 180, 36);
            btnExit.Click += (s, e) =>
            {
                _config.Save();
                System.Windows.Application.Current.Shutdown();
            };
            actStack.Children.Add(btnExit);

            stack.Children.Add(CreateSettingCard("Application Controls", "Save your preferences or shut down the background process.", actStack));

            scroll.Content = stack;
            return scroll;
        }

        // ================= UI BUILDER HELPERS =================
        private Border CreateSettingCard(string title, string subtitle, UIElement content)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 25, 38)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 44, 68)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(18, 14, 18, 16),
                Margin = new Thickness(0, 0, 0, 14)
            };

            var mainStack = new StackPanel();

            var titleTxt = new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 0, 0, 2)
            };
            mainStack.Children.Add(titleTxt);

            if (!string.IsNullOrEmpty(subtitle))
            {
                var subTxt = new TextBlock
                {
                    Text = subtitle,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184)),
                    Margin = new Thickness(0, 0, 0, 8)
                };
                mainStack.Children.Add(subTxt);
            }

            if (content != null)
            {
                mainStack.Children.Add(content);
            }

            border.Child = mainStack;
            return border;
        }

        private System.Windows.Controls.Button CreateActionButton(string text, string hexColor, double width, double height)
        {
            var btn = new System.Windows.Controls.Button
            {
                Content = text,
                Width = width,
                Height = height,
                Margin = new Thickness(0, 0, 8, 0),
                Foreground = System.Windows.Media.Brushes.White,
                Background = (SolidColorBrush)new BrushConverter().ConvertFrom(hexColor),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12
            };
            return btn;
        }

        private System.Windows.Controls.ComboBox CreateStyledComboBox(double width)
        {
            var cb = new System.Windows.Controls.ComboBox
            {
                Width = width,
                Height = 32,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 33, 50)),
                Foreground = System.Windows.Media.Brushes.Black,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left
            };
            return cb;
        }

        private System.Windows.Controls.CheckBox CreateStyledCheckBox(string content, double fontSize, bool bold)
        {
            var chk = new System.Windows.Controls.CheckBox
            {
                Content = content,
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = fontSize,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                Cursor = System.Windows.Input.Cursors.Hand,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            return chk;
        }

        private StackPanel CreateEmptyPlaceholder(string title, string subtitle)
        {
            var stack = new StackPanel
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };

            var icon = new TextBlock
            {
                Text = "🖼️",
                FontSize = 40,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8),
                Opacity = 0.7
            };
            var t = new TextBlock
            {
                Text = title,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225)),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };
            var s = new TextBlock
            {
                Text = subtitle,
                FontSize = 12,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184)),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            };

            stack.Children.Add(icon);
            stack.Children.Add(t);
            stack.Children.Add(s);
            return stack;
        }

        private DataTemplate CreateImageItemDataTemplate()
        {
            var dt = new DataTemplate(typeof(ImageListItem));
            var factory = new FrameworkElementFactory(typeof(Border));
            factory.SetValue(Border.BackgroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(28, 31, 48)));
            factory.SetValue(Border.BorderBrushProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 49, 74)));
            factory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            factory.SetValue(Border.PaddingProperty, new Thickness(8, 6, 8, 6));
            factory.SetValue(Border.MarginProperty, new Thickness(0, 0, 0, 6));

            var stack = new FrameworkElementFactory(typeof(DockPanel));

            // Thumbnail
            var img = new FrameworkElementFactory(typeof(System.Windows.Controls.Image));
            img.SetValue(System.Windows.Controls.Image.WidthProperty, 56.0);
            img.SetValue(System.Windows.Controls.Image.HeightProperty, 38.0);
            img.SetValue(System.Windows.Controls.Image.StretchProperty, Stretch.UniformToFill);
            img.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 10, 0));
            img.SetBinding(System.Windows.Controls.Image.SourceProperty, new System.Windows.Data.Binding("Thumbnail"));
            img.SetValue(DockPanel.DockProperty, System.Windows.Controls.Dock.Left);
            stack.AppendChild(img);

            // Text Info
            var textStack = new FrameworkElementFactory(typeof(StackPanel));
            textStack.SetValue(StackPanel.VerticalAlignmentProperty, VerticalAlignment.Center);

            var nameTxt = new FrameworkElementFactory(typeof(TextBlock));
            nameTxt.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            nameTxt.SetValue(TextBlock.FontSizeProperty, 13.0);
            nameTxt.SetValue(TextBlock.ForegroundProperty, System.Windows.Media.Brushes.White);
            nameTxt.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("FileName"));
            textStack.AppendChild(nameTxt);

            var pathTxt = new FrameworkElementFactory(typeof(TextBlock));
            pathTxt.SetValue(TextBlock.FontSizeProperty, 11.0);
            pathTxt.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184)));
            pathTxt.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("FilePath"));
            textStack.AppendChild(pathTxt);

            stack.AppendChild(textStack);
            factory.AppendChild(stack);
            dt.VisualTree = factory;
            return dt;
        }

        // ================= DATA BINDING & FILE ACTIONS =================
        private void LoadMonitorsAndData()
        {
            _monitors = WallpaperManager.GetAllMonitors();
            _cbMonitors.Items.Clear();

            while (_config.Monitors.Count < _monitors.Count)
            {
                int idx = _config.Monitors.Count;
                _config.Monitors.Add(new MonitorConfig
                {
                    DevicePath = _monitors[idx].DevicePath,
                    FriendlyName = _monitors[idx].DisplayName
                });
            }

            for (int i = 0; i < _monitors.Count; i++)
            {
                _cbMonitors.Items.Add(_monitors[i].DisplayName);
                if (i < _config.Monitors.Count)
                {
                    _config.Monitors[i].DevicePath = _monitors[i].DevicePath;
                    _config.Monitors[i].FriendlyName = _monitors[i].DisplayName;
                }
            }

            if (_cbMonitors.Items.Count > 0)
                _cbMonitors.SelectedIndex = 0;

            _config.Save();
            RefreshLockScreenList();
        }

        private MonitorConfig GetCurrentMonitorConfig()
        {
            if (_selectedMonitorIndex >= 0 && _selectedMonitorIndex < _config.Monitors.Count)
                return _config.Monitors[_selectedMonitorIndex];
            return null;
        }

        private void OnMonitorChanged()
        {
            _selectedMonitorIndex = _cbMonitors.SelectedIndex;
            var mon = GetCurrentMonitorConfig();
            if (mon != null)
            {
                _cbMonitorMode.SelectedIndex = mon.Mode == "Sequential" ? 1 : 0;
                RefreshMonitorImageList();
            }
        }

        private void RefreshMonitorImageList()
        {
            _lbMonitorImages.Items.Clear();
            var mon = GetCurrentMonitorConfig();
            if (mon != null)
            {
                foreach (var path in mon.ImagePaths)
                {
                    _lbMonitorImages.Items.Add(new ImageListItem
                    {
                        FilePath = path,
                        FileName = Path.GetFileName(path),
                        Thumbnail = TransitionEngine.LoadBitmapSafe(path)
                    });
                }
                _tbMonitorCount.Text = mon.ImagePaths.Count + " images configured";
                _emptyMonitorPlaceholder.Visibility = mon.ImagePaths.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void RefreshLockScreenList()
        {
            _lbLockImages.Items.Clear();
            foreach (var path in _config.LockScreen.ImagePaths)
            {
                _lbLockImages.Items.Add(new ImageListItem
                {
                    FilePath = path,
                    FileName = Path.GetFileName(path),
                    Thumbnail = TransitionEngine.LoadBitmapSafe(path)
                });
            }
            _tbLockCount.Text = _config.LockScreen.ImagePaths.Count + " images for Lock Screen";
            _emptyLockPlaceholder.Visibility = _config.LockScreen.ImagePaths.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AddImagesToCurrentMonitor()
        {
            var mon = GetCurrentMonitorConfig();
            if (mon == null) return;

            var dlg = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.webp|All Files|*.*",
                Multiselect = true,
                Title = "Select images for this monitor"
            };

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                foreach (var file in dlg.FileNames)
                {
                    if (!mon.ImagePaths.Contains(file))
                    {
                        mon.ImagePaths.Add(file);
                    }
                }
                _config.Save();
                RefreshMonitorImageList();
            }
        }

        private void AddFolderToCurrentMonitor()
        {
            var mon = GetCurrentMonitorConfig();
            if (mon == null) return;

            var dlg = new FolderBrowserDialog
            {
                Description = "Select a folder containing images for this monitor"
            };

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var files = Directory.GetFiles(dlg.SelectedPath, "*.*", SearchOption.AllDirectories);
                int added = 0;
                foreach (var file in files)
                {
                    string ext = Path.GetExtension(file).ToLower();
                    if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".webp")
                    {
                        if (!mon.ImagePaths.Contains(file))
                        {
                            mon.ImagePaths.Add(file);
                            added++;
                        }
                    }
                }
                _config.Save();
                RefreshMonitorImageList();
                System.Windows.MessageBox.Show("Added " + added + " images from folder!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RemoveSelectedImagesFromCurrentMonitor()
        {
            var mon = GetCurrentMonitorConfig();
            if (mon == null) return;

            var itemsToRemove = new List<ImageListItem>();
            foreach (ImageListItem item in _lbMonitorImages.SelectedItems)
            {
                itemsToRemove.Add(item);
            }

            foreach (var item in itemsToRemove)
            {
                mon.ImagePaths.Remove(item.FilePath);
            }

            _config.Save();
            RefreshMonitorImageList();
        }

        private void ClearImagesFromCurrentMonitor()
        {
            var mon = GetCurrentMonitorConfig();
            if (mon == null) return;

            if (System.Windows.MessageBox.Show("Are you sure you want to clear all images for this monitor?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                mon.ImagePaths.Clear();
                _config.Save();
                RefreshMonitorImageList();
            }
        }

        private void AddImagesToLockScreen()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.webp|All Files|*.*",
                Multiselect = true,
                Title = "Select images for Lock Screen"
            };

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                foreach (var file in dlg.FileNames)
                {
                    if (!_config.LockScreen.ImagePaths.Contains(file))
                    {
                        _config.LockScreen.ImagePaths.Add(file);
                    }
                }
                _config.Save();
                RefreshLockScreenList();
            }
        }

        private void AddFolderToLockScreen()
        {
            var dlg = new FolderBrowserDialog
            {
                Description = "Select a folder containing images for Lock Screen"
            };

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var files = Directory.GetFiles(dlg.SelectedPath, "*.*", SearchOption.AllDirectories);
                int added = 0;
                foreach (var file in files)
                {
                    string ext = Path.GetExtension(file).ToLower();
                    if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".webp")
                    {
                        if (!_config.LockScreen.ImagePaths.Contains(file))
                        {
                            _config.LockScreen.ImagePaths.Add(file);
                            added++;
                        }
                    }
                }
                _config.Save();
                RefreshLockScreenList();
                System.Windows.MessageBox.Show("Added " + added + " images for Lock Screen!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RemoveSelectedImagesFromLockScreen()
        {
            var itemsToRemove = new List<ImageListItem>();
            foreach (ImageListItem item in _lbLockImages.SelectedItems)
            {
                itemsToRemove.Add(item);
            }

            foreach (var item in itemsToRemove)
            {
                _config.LockScreen.ImagePaths.Remove(item.FilePath);
            }

            _config.Save();
            RefreshLockScreenList();
        }

        private void SelectComboItemByTag(System.Windows.Controls.ComboBox cb, string tag)
        {
            foreach (ComboBoxItem item in cb.Items)
            {
                if (item.Tag != null && item.Tag.ToString() == tag)
                {
                    cb.SelectedItem = item;
                    return;
                }
            }
            if (cb.Items.Count > 0) cb.SelectedIndex = 0;
        }

        private void PopulateIntervalCombo(System.Windows.Controls.ComboBox cb)
        {
            cb.Items.Clear();
            cb.Items.Add(new ComboBoxItem { Content = "10 seconds", Tag = 10 });
            cb.Items.Add(new ComboBoxItem { Content = "20 seconds", Tag = 20 });
            cb.Items.Add(new ComboBoxItem { Content = "30 seconds", Tag = 30 });
            cb.Items.Add(new ComboBoxItem { Content = "1 minute", Tag = 60 });
            cb.Items.Add(new ComboBoxItem { Content = "2 minutes", Tag = 120 });
            cb.Items.Add(new ComboBoxItem { Content = "3 minutes", Tag = 180 });
            cb.Items.Add(new ComboBoxItem { Content = "5 minutes", Tag = 300 });
            cb.Items.Add(new ComboBoxItem { Content = "10 minutes", Tag = 600 });
            cb.Items.Add(new ComboBoxItem { Content = "15 minutes", Tag = 900 });
            cb.Items.Add(new ComboBoxItem { Content = "30 minutes", Tag = 1800 });
            cb.Items.Add(new ComboBoxItem { Content = "1 hour", Tag = 3600 });
            cb.Items.Add(new ComboBoxItem { Content = "2 hours", Tag = 7200 });
        }

        private void SelectComboItemByIntTag(System.Windows.Controls.ComboBox cb, int tag)
        {
            foreach (ComboBoxItem item in cb.Items)
            {
                if (item.Tag != null && (int)item.Tag == tag)
                {
                    cb.SelectedItem = item;
                    return;
                }
            }
            if (cb.Items.Count > 3) cb.SelectedIndex = 3;
        }

        public void UpdateStatus(string message)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                _tbStatus.Text = "● " + message;
            }));
        }

        public void ShowWelcomeDialog()
        {
            var welcomeWin = new Window
            {
                Title = "Welcome to MultiWall FX",
                Width = 470,
                Height = 310,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 17, 26)),
                Foreground = System.Windows.Media.Brushes.White,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true
            };

            var container = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 25, 38)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(99, 102, 241)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(24)
            };

            var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var iconTxt = new TextBlock
            {
                Text = "🐙",
                FontSize = 44,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stack.Children.Add(iconTxt);

            var titleTxt = new TextBlock
            {
                Text = "Welcome to MultiWall FX!",
                FontSize = 19,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };
            stack.Children.Add(titleTxt);

            var subtitleTxt = new TextBlock
            {
                Text = "This project was made by v1te0k.",
                FontSize = 13,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225)),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            stack.Children.Add(subtitleTxt);

            var linkBtn = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(45, 99, 102, 241)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(140, 129, 140, 248)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 6, 14, 6),
                Margin = new Thickness(0, 2, 0, 20),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "Click to visit https://github.com/v1te0k"
            };
            var linkStack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
            linkStack.Children.Add(new TextBlock
            {
                Text = "🐙 https://github.com/v1te0k ↗",
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(165, 180, 252)),
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                TextDecorations = TextDecorations.Underline
            });
            linkBtn.Child = linkStack;
            linkBtn.MouseDown += (s, e) =>
            {
                try { System.Diagnostics.Process.Start("https://github.com/v1te0k"); } catch { }
            };
            stack.Children.Add(linkBtn);

            var btnPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Center };

            var btnVisit = CreateActionButton("🐙 Visit GitHub", "#4f46e5", 140, 36);
            btnVisit.Click += (s, e) =>
            {
                try { System.Diagnostics.Process.Start("https://github.com/v1te0k"); } catch { }
                welcomeWin.Close();
            };
            btnPanel.Children.Add(btnVisit);

            var btnClose = CreateActionButton("🚀 Get Started", "#22c55e", 140, 36);
            btnClose.Margin = new Thickness(12, 0, 0, 0);
            btnClose.Click += (s, e) => welcomeWin.Close();
            btnPanel.Children.Add(btnClose);

            stack.Children.Add(btnPanel);
            container.Child = stack;
            welcomeWin.Content = container;

            welcomeWin.ShowDialog();
        }
    }
}
