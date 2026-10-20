using System;

namespace NaturalClicker
{
    public enum MouseButtonKind
    {
        Left = 0,
        Right = 1,
        Middle = 2
    }

    public enum IntervalDistribution
    {
        Human = 0,
        Uniform = 1,
        Fixed = 2
    }

    [Serializable]
    public class AppSettings
    {
        public int Version { get; set; }
        public MouseButtonKind Button { get; set; }
        public bool FixedPosition { get; set; }
        public int FixedX { get; set; }
        public int FixedY { get; set; }
        public int IntervalMinMs { get; set; }
        public int IntervalMaxMs { get; set; }
        public IntervalDistribution Distribution { get; set; }
        public int HoldMinMs { get; set; }
        public int HoldMaxMs { get; set; }
        public int PositionJitter { get; set; }
        public int RhythmSmooth { get; set; }
        public double MicroPauseChance { get; set; }
        public int MicroPauseMs { get; set; }
        public double LongPauseChance { get; set; }
        public int LongPauseMs { get; set; }
        public int CountdownSeconds { get; set; }
        public int MaxClicks { get; set; }
        public int Seed { get; set; }
        public string Hotkey { get; set; }
        public bool MinimizeOnStart { get; set; }
        public bool AlwaysOnTop { get; set; }

        public AppSettings()
        {
            Version = 1;
            Button = MouseButtonKind.Left;
            FixedPosition = false;
            FixedX = 0;
            FixedY = 0;
            IntervalMinMs = 120;
            IntervalMaxMs = 260;
            Distribution = IntervalDistribution.Human;
            HoldMinMs = 40;
            HoldMaxMs = 90;
            PositionJitter = 0;
            RhythmSmooth = 55;
            MicroPauseChance = 3.0;
            MicroPauseMs = 180;
            LongPauseChance = 0.25;
            LongPauseMs = 1600;
            CountdownSeconds = 3;
            MaxClicks = 0;
            Seed = 0;
            Hotkey = "F6";
            MinimizeOnStart = true;
            AlwaysOnTop = false;
        }

        public AppSettings Clone()
        {
            return (AppSettings)MemberwiseClone();
        }

        public void CopyTimingFrom(AppSettings other)
        {
            IntervalMinMs = other.IntervalMinMs;
            IntervalMaxMs = other.IntervalMaxMs;
            Distribution = other.Distribution;
            HoldMinMs = other.HoldMinMs;
            HoldMaxMs = other.HoldMaxMs;
            RhythmSmooth = other.RhythmSmooth;
            MicroPauseChance = other.MicroPauseChance;
            MicroPauseMs = other.MicroPauseMs;
            LongPauseChance = other.LongPauseChance;
            LongPauseMs = other.LongPauseMs;
        }

        public static AppSettings NaturalPreset()
        {
            AppSettings s = new AppSettings();
            return s;
        }

        public static AppSettings LightPreset()
        {
            AppSettings s = new AppSettings();
            s.IntervalMinMs = 220;
            s.IntervalMaxMs = 380;
            s.HoldMinMs = 50;
            s.HoldMaxMs = 105;
            s.RhythmSmooth = 65;
            s.MicroPauseChance = 5.0;
            s.MicroPauseMs = 220;
            s.LongPauseChance = 0.6;
            s.LongPauseMs = 1800;
            return s;
        }

        public static AppSettings FastPreset()
        {
            AppSettings s = new AppSettings();
            s.IntervalMinMs = 35;
            s.IntervalMaxMs = 75;
            s.HoldMinMs = 20;
            s.HoldMaxMs = 45;
            s.RhythmSmooth = 28;
            s.MicroPauseChance = 0.5;
            s.MicroPauseMs = 110;
            s.LongPauseChance = 0.0;
            s.LongPauseMs = 900;
            return s;
        }

        public static AppSettings TurboPreset()
        {
            AppSettings s = new AppSettings();
            s.IntervalMinMs = 15;
            s.IntervalMaxMs = 30;
            s.Distribution = IntervalDistribution.Uniform;
            s.HoldMinMs = 12;
            s.HoldMaxMs = 24;
            s.RhythmSmooth = 0;
            s.MicroPauseChance = 0.0;
            s.MicroPauseMs = 80;
            s.LongPauseChance = 0.0;
            s.LongPauseMs = 800;
            return s;
        }

        public static AppSettings SlowPreset()
        {
            AppSettings s = new AppSettings();
            s.IntervalMinMs = 500;
            s.IntervalMaxMs = 800;
            s.HoldMinMs = 60;
            s.HoldMaxMs = 125;
            s.RhythmSmooth = 70;
            s.MicroPauseChance = 8.0;
            s.MicroPauseMs = 300;
            s.LongPauseChance = 1.2;
            s.LongPauseMs = 2500;
            return s;
        }
    }
}
