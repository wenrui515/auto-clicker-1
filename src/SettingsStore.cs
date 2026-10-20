using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace NaturalClicker
{
    internal class PresetInfo
    {
        public string Name { get; set; }
        public string FilePath { get; set; }

        public PresetInfo(string name, string filePath)
        {
            Name = name;
            FilePath = filePath;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    internal static class SettingsStore
    {
        private static readonly object SyncRoot = new object();
        private static string _dataDirectory;

        public static string DataDirectory
        {
            get
            {
                lock (SyncRoot)
                {
                    if (_dataDirectory != null)
                    {
                        return _dataDirectory;
                    }

                    string overrideDirectory = Environment.GetEnvironmentVariable("NATURALCLICKER_DATA_DIR");
                    if (!string.IsNullOrEmpty(overrideDirectory) && IsWritableDirectory(overrideDirectory))
                    {
                        _dataDirectory = overrideDirectory;
                        return _dataDirectory;
                    }

                    string roaming = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "NaturalClicker");
                    if (IsWritableDirectory(roaming))
                    {
                        _dataDirectory = roaming;
                        return _dataDirectory;
                    }

                    string local = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "NaturalClicker");
                    if (IsWritableDirectory(local))
                    {
                        _dataDirectory = local;
                        return _dataDirectory;
                    }

                    string portable = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NaturalClicker-Data");
                    try
                    {
                        Directory.CreateDirectory(portable);
                    }
                    catch
                    {
                    }
                    _dataDirectory = portable;
                    return _dataDirectory;
                }
            }
        }

        private static bool IsWritableDirectory(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                string probe = Path.Combine(path, ".write-test-" + Guid.NewGuid().ToString("N"));
                File.WriteAllText(probe, "ok");
                File.Delete(probe);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string PresetDirectory
        {
            get
            {
                string dir = Path.Combine(DataDirectory, "presets");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static string SettingsPath
        {
            get { return Path.Combine(DataDirectory, "settings.xml"); }
        }

        public static AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    return Deserialize(SettingsPath);
                }
            }
            catch
            {
                // A damaged settings file should never prevent the app from starting.
            }
            return new AppSettings();
        }

        public static void SaveSettings(AppSettings settings)
        {
            lock (SyncRoot)
            {
                Serialize(settings, SettingsPath);
            }
        }

        public static List<PresetInfo> ListPresets()
        {
            List<PresetInfo> list = new List<PresetInfo>();
            try
            {
                string dir = PresetDirectory;
                foreach (string file in Directory.GetFiles(dir, "*.xml"))
                {
                    list.Add(new PresetInfo(Path.GetFileNameWithoutExtension(file), file));
                }
            }
            catch
            {
            }
            list.Sort(delegate(PresetInfo a, PresetInfo b)
            {
                return string.Compare(a.Name, b.Name, StringComparison.CurrentCulture);
            });
            return list;
        }

        public static PresetInfo SavePreset(string name, AppSettings settings)
        {
            string safeName = SanitizeName(name);
            string path = Path.Combine(PresetDirectory, safeName + ".xml");
            lock (SyncRoot)
            {
                Serialize(settings, path);
            }
            return new PresetInfo(safeName, path);
        }

        public static AppSettings LoadPreset(string path)
        {
            return Deserialize(path);
        }

        private static string SanitizeName(string name)
        {
            string value = (name ?? string.Empty).Trim();
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }
            value = value.Replace('.', '_').Trim();
            if (value.Length == 0)
            {
                value = "未命名方案";
            }
            if (value.Length > 40)
            {
                value = value.Substring(0, 40);
            }
            return value;
        }

        private static void Serialize(AppSettings settings, string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
            string temp = path + ".tmp";

            XmlWriterSettings writerSettings = new XmlWriterSettings();
            writerSettings.Indent = true;
            writerSettings.Encoding = new UTF8Encoding(false);
            using (XmlWriter writer = XmlWriter.Create(temp, writerSettings))
            {
                serializer.Serialize(writer, settings);
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }
            File.Move(temp, path);
        }

        private static AppSettings Deserialize(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
            using (StreamReader reader = new StreamReader(path, Encoding.UTF8))
            {
                AppSettings settings = serializer.Deserialize(reader) as AppSettings;
                if (settings == null)
                {
                    return new AppSettings();
                }
                Normalize(settings);
                return settings;
            }
        }

        private static void Normalize(AppSettings s)
        {
            uint mods;
            uint vk;
            if (s.Hotkey == null || !HotkeyCatalog.TryParse(s.Hotkey, out mods, out vk))
            {
                s.Hotkey = "F6";
            }
            if (!Enum.IsDefined(typeof(MouseButtonKind), s.Button))
            {
                s.Button = MouseButtonKind.Left;
            }
            if (!Enum.IsDefined(typeof(IntervalDistribution), s.Distribution))
            {
                s.Distribution = IntervalDistribution.Human;
            }
            s.IntervalMinMs = Clamp(s.IntervalMinMs, 5, 60000);
            s.IntervalMaxMs = Clamp(s.IntervalMaxMs, s.IntervalMinMs, 60000);
            s.HoldMinMs = Clamp(s.HoldMinMs, 1, 5000);
            s.HoldMaxMs = Clamp(s.HoldMaxMs, s.HoldMinMs, 5000);
            s.PositionJitter = Clamp(s.PositionJitter, 0, 100);
            s.RhythmSmooth = Clamp(s.RhythmSmooth, 0, 100);
            s.MicroPauseChance = Clamp(s.MicroPauseChance, 0.0, 100.0);
            s.MicroPauseMs = Clamp(s.MicroPauseMs, 10, 60000);
            s.LongPauseChance = Clamp(s.LongPauseChance, 0.0, 100.0);
            s.LongPauseMs = Clamp(s.LongPauseMs, 10, 60000);
            s.CountdownSeconds = Clamp(s.CountdownSeconds, 1, 60);
            s.MaxClicks = Clamp(s.MaxClicks, 0, 100000000);
            s.Seed = Clamp(s.Seed, 0, int.MaxValue);
            if (s.FixedX < -32768 || s.FixedX > 32767) s.FixedX = 0;
            if (s.FixedY < -32768 || s.FixedY > 32767) s.FixedY = 0;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (double.IsNaN(value)) return min;
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
