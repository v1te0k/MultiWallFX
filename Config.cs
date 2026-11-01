using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace MultiWallFX
{
    public class MonitorConfig
    {
        public string DevicePath { get; set; }
        public string FriendlyName { get; set; }
        public List<string> ImagePaths { get; set; }
        public string Mode { get; set; } // "Random", "Sequential"
        public int CurrentIndex { get; set; }
        public string LastAppliedImage { get; set; }

        public MonitorConfig()
        {
            DevicePath = "";
            FriendlyName = "";
            ImagePaths = new List<string>();
            Mode = "Random";
            CurrentIndex = 0;
            LastAppliedImage = "";
        }
    }

    public class LockScreenConfig
    {
        public bool Enabled { get; set; }
        public bool RandomizeOnStartup { get; set; }
        public bool RandomizeOnTimer { get; set; }
        public List<string> ImagePaths { get; set; }
        public string LastAppliedImage { get; set; }

        public LockScreenConfig()
        {
            Enabled = true;
            RandomizeOnStartup = true;
            RandomizeOnTimer = false;
            ImagePaths = new List<string>();
            LastAppliedImage = "";
        }
    }

    public class AppConfig
    {
        public bool StartWithWindows { get; set; }
        public bool HasSeenWelcome { get; set; }
        public bool RandomizeOnStartup { get; set; }
        public bool EnableSlideshow { get; set; }
        public int IntervalSeconds { get; set; }
        public bool PauseWhileGaming { get; set; }

        // Random Interval & Photo
        public bool RandomInterval { get; set; }
        public int RandomIntervalMinSeconds { get; set; }
        public int RandomIntervalMaxSeconds { get; set; }
        public bool RandomPhoto { get; set; }

        // Transitions & Performance
        public bool EnableTransitions { get; set; }
        public bool DisableAnimationWhenCovered { get; set; }
        public string TransitionType { get; set; } // "Fade", "SlideLeft", "SlideRight", "SlideUp", "SlideDown", "Zoom", "Random"
        public double TransitionDurationMs { get; set; }
        public string EasingType { get; set; } // "Cubic", "Quadratic", "Sine", "Bounce", "Linear"

        public List<MonitorConfig> Monitors { get; set; }
        public LockScreenConfig LockScreen { get; set; }

        public AppConfig()
        {
            StartWithWindows = false;
            RandomizeOnStartup = true;
            EnableSlideshow = true;
            IntervalSeconds = 300; // 5 minutes
            PauseWhileGaming = true;

            RandomInterval = true;
            RandomIntervalMinSeconds = 30; // 30 sec
            RandomIntervalMaxSeconds = 300; // 5 minutes
            RandomPhoto = true;

            EnableTransitions = true;
            DisableAnimationWhenCovered = true;
            TransitionType = "Fade";
            TransitionDurationMs = 1200;
            EasingType = "Cubic";

            Monitors = new List<MonitorConfig>();
            LockScreen = new LockScreenConfig();
        }

        private static string ConfigPath
        {
            get
            {
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(dir, "config.json");
            }
        }

        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var serializer = new JavaScriptSerializer();
                    var cfg = serializer.Deserialize<AppConfig>(json);
                    if (cfg != null)
                    {
                        if (cfg.RandomIntervalMinSeconds <= 0) cfg.RandomIntervalMinSeconds = 30;
                        if (cfg.RandomIntervalMaxSeconds <= 0) cfg.RandomIntervalMaxSeconds = 300;
                        if (cfg.RandomIntervalMaxSeconds <= cfg.RandomIntervalMinSeconds) cfg.RandomIntervalMaxSeconds = cfg.RandomIntervalMinSeconds + 60;
                        return cfg;
                    }
                }
            }
            catch { }
            var def = new AppConfig();
            def.Save();
            return def;
        }

        public void Save()
        {
            try
            {
                var serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(this);
                File.WriteAllText(ConfigPath, json);
            }
            catch { }
        }
    }
}
