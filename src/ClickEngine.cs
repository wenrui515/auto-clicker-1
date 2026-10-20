using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace NaturalClicker
{
    internal enum RunEndKind
    {
        Stopped,
        Completed,
        Blocked,
        Error
    }

    internal sealed class CountdownEventArgs : EventArgs
    {
        public int Remaining { get; private set; }
        public int Total { get; private set; }

        public CountdownEventArgs(int remaining, int total)
        {
            Remaining = remaining;
            Total = total;
        }
    }

    internal sealed class ClickSnapshot
    {
        public long Count { get; set; }
        public double ElapsedSeconds { get; set; }
        public double LastIntervalMs { get; set; }
        public double AverageIntervalMs { get; set; }
        public double ClicksPerSecond { get; set; }
        public double MinIntervalMs { get; set; }
        public double MaxIntervalMs { get; set; }
        public int Seed { get; set; }
        public double[] RecentIntervals { get; set; }
    }

    internal sealed class RunResult
    {
        public RunEndKind Kind { get; set; }
        public string Message { get; set; }
        public long Count { get; set; }
        public double ElapsedSeconds { get; set; }
        public int Seed { get; set; }
    }

    internal sealed class ClickEngine
    {
        private readonly object _syncRoot = new object();
        private AppSettings _settings;
        private Thread _thread;
        private IntPtr _ownerHandle;
        private volatile bool _active;
        private volatile bool _stopRequested;
        private int _seed;
        private long _count;

        public event EventHandler<CountdownEventArgs> CountdownTick;
        public event EventHandler<ClickSnapshot> Clicked;
        public event EventHandler<RunResult> Finished;
        public event EventHandler<int> SeedReady;

        public bool IsActive
        {
            get { return _active; }
        }

        public long Count
        {
            get { return Interlocked.Read(ref _count); }
        }

        public bool Start(AppSettings settings, IntPtr ownerHandle)
        {
            if (settings == null)
            {
                return false;
            }

            lock (_syncRoot)
            {
                if (_active)
                {
                    return false;
                }

                _settings = settings.Clone();
                _ownerHandle = ownerHandle;
                _stopRequested = false;
                _count = 0;
                _seed = ResolveSeed(settings.Seed);
                _active = true;
                _thread = new Thread(Run);
                _thread.IsBackground = true;
                _thread.Name = "NaturalClickEngine";
                _thread.Start();
                return true;
            }
        }

        public void Stop()
        {
            _stopRequested = true;
        }

        public bool StopAndWait(int timeoutMilliseconds)
        {
            _stopRequested = true;
            Thread thread;
            lock (_syncRoot)
            {
                thread = _thread;
            }
            if (thread == null || !thread.IsAlive)
            {
                return true;
            }
            return thread.Join(Math.Max(1, timeoutMilliseconds));
        }

        private static int ResolveSeed(int configuredSeed)
        {
            if (configuredSeed > 0)
            {
                return configuredSeed;
            }

            byte[] bytes = new byte[4];
            using (System.Security.Cryptography.RandomNumberGenerator rng =
                System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            int seed = BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF;
            return seed == 0 ? 1 : seed;
        }

        private void Run()
        {
            Native.timeBeginPeriod(1);
            AppSettings settings = _settings;
            Random random = new Random(_seed);
            Stopwatch totalWatch = Stopwatch.StartNew();
            string message = "已停止";
            RunEndKind kind = RunEndKind.Stopped;
            bool buttonDown = false;
            long count = 0;
            long lastDownTimestamp = 0;
            double intervalSum = 0;
            double lastInterval = 0;
            double minInterval = double.MaxValue;
            double maxInterval = 0;
            int intervalSamples = 0;
            List<double> recent = new List<double>();

            try
            {
                RaiseSeedReady(_seed);

                for (int remaining = settings.CountdownSeconds; remaining > 0; remaining--)
                {
                    RaiseCountdown(remaining, settings.CountdownSeconds);
                    if (!WaitMs(1000))
                    {
                        break;
                    }
                }

                if (!_stopRequested)
                {
                    RaiseCountdown(0, settings.CountdownSeconds);
                    Stopwatch runWatch = Stopwatch.StartNew();
                    double drift = 0.0;

                    while (!_stopRequested)
                    {
                        if (settings.MaxClicks > 0 && count >= settings.MaxClicks)
                        {
                            kind = RunEndKind.Completed;
                            message = "已达到设定的点击次数";
                            break;
                        }

                        if (count > 0)
                        {
                            int interval = NextInterval(random, ref drift, settings);
                            long target = lastDownTimestamp + MsToTicks(interval);
                            if (!WaitUntil(target))
                            {
                                message = "已停止";
                                break;
                            }
                        }

                        Point targetPoint = GetTargetPoint(random, settings);
                        Rectangle ownerRect = Rectangle.Empty;
                        if (IsPointInsideOwner(targetPoint, out ownerRect))
                        {
                            kind = RunEndKind.Blocked;
                            message = "点击位置被连点器窗口遮挡，已自动停止。请最小化窗口或使用固定坐标。";
                            break;
                        }

                        if (settings.FixedPosition)
                        {
                            if (!Native.MoveAbsolute(targetPoint))
                            {
                                kind = RunEndKind.Error;
                                message = "无法移动鼠标，请检查屏幕坐标或权限设置。";
                                break;
                            }
                        }

                        long downTimestamp = Stopwatch.GetTimestamp();
                        if (!SendMouseDown(settings.Button))
                        {
                            kind = RunEndKind.Error;
                            message = "无法发送鼠标按下事件。若目标程序以管理员身份运行，请同样以管理员身份运行本程序。";
                            break;
                        }
                        buttonDown = true;

                        int hold = NextHold(random, settings);
                        bool holdFinished = WaitMs(hold);
                        bool upSent = SendMouseUp(settings.Button);
                        buttonDown = false;

                        if (!upSent)
                        {
                            kind = RunEndKind.Error;
                            message = "无法发送鼠标抬起事件，已停止以避免鼠标卡住。";
                            break;
                        }

                        if (!holdFinished)
                        {
                            message = "已停止";
                            break;
                        }

                        count++;
                        Interlocked.Exchange(ref _count, count);

                        if (lastDownTimestamp != 0)
                        {
                            double actual = (downTimestamp - lastDownTimestamp) * 1000.0 / Stopwatch.Frequency;
                            if (actual > 0)
                            {
                                lastInterval = actual;
                                intervalSum += actual;
                                intervalSamples++;
                                if (actual < minInterval) minInterval = actual;
                                if (actual > maxInterval) maxInterval = actual;
                                recent.Add(actual);
                                if (recent.Count > 56)
                                {
                                    recent.RemoveAt(0);
                                }
                            }
                        }
                        lastDownTimestamp = downTimestamp;

                        ClickSnapshot snapshot = new ClickSnapshot();
                        snapshot.Count = count;
                        snapshot.ElapsedSeconds = runWatch.Elapsed.TotalSeconds;
                        snapshot.LastIntervalMs = lastInterval;
                        snapshot.AverageIntervalMs = intervalSamples > 0 ? intervalSum / intervalSamples : 0;
                        snapshot.ClicksPerSecond = intervalSamples > 0 && intervalSum > 0
                            ? intervalSamples * 1000.0 / intervalSum
                            : 0;
                        snapshot.MinIntervalMs = intervalSamples > 0 ? minInterval : 0;
                        snapshot.MaxIntervalMs = maxInterval;
                        snapshot.Seed = _seed;
                        snapshot.RecentIntervals = recent.ToArray();
                        RaiseClicked(snapshot);

                        if (_stopRequested)
                        {
                            message = "已停止";
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                kind = RunEndKind.Error;
                message = "运行出错：" + ex.Message;
            }
            finally
            {
                if (buttonDown)
                {
                    try
                    {
                        SendMouseUp(settings.Button);
                    }
                    catch
                    {
                    }
                }
                Native.timeEndPeriod(1);

                RunResult result = new RunResult();
                result.Kind = kind;
                result.Message = message;
                result.Count = count;
                result.ElapsedSeconds = totalWatch.Elapsed.TotalSeconds;
                result.Seed = _seed;

                _active = false;
                lock (_syncRoot)
                {
                    _thread = null;
                }
                RaiseFinished(result);
            }
        }

        private static Point GetTargetPoint(Random random, AppSettings settings)
        {
            if (!settings.FixedPosition)
            {
                return Native.GetCursorPosition();
            }

            int jitter = settings.PositionJitter;
            int x = settings.FixedX;
            int y = settings.FixedY;
            if (jitter > 0)
            {
                x += random.Next(-jitter, jitter + 1);
                y += random.Next(-jitter, jitter + 1);
            }
            return new Point(x, y);
        }

        private bool IsPointInsideOwner(Point point, out Rectangle ownerRect)
        {
            ownerRect = Rectangle.Empty;
            if (_ownerHandle == IntPtr.Zero || !Native.IsWindowOnScreen(_ownerHandle))
            {
                return false;
            }
            ownerRect = Native.GetWindowRectangle(_ownerHandle);
            return !ownerRect.IsEmpty && ownerRect.Contains(point);
        }

        private static int NextInterval(Random random, ref double drift, AppSettings settings)
        {
            int min = Math.Min(settings.IntervalMinMs, settings.IntervalMaxMs);
            int max = Math.Max(settings.IntervalMinMs, settings.IntervalMaxMs);
            if (min < 1) min = 1;
            if (max < min) max = min;

            if (settings.Distribution == IntervalDistribution.Fixed)
            {
                return Math.Max(1, (min + max) / 2);
            }

            double value;
            if (settings.Distribution == IntervalDistribution.Uniform)
            {
                value = min + random.NextDouble() * (max - min);
            }
            else
            {
                double center = (min + max) / 2.0;
                double sigma = Math.Max(0.8, (max - min) / 6.0);
                double smooth = Math.Max(0, Math.Min(100, settings.RhythmSmooth)) / 100.0;
                double rho = 0.15 + 0.75 * smooth;
                if (rho > 0.94) rho = 0.94;

                double gaussian = NextGaussian(random);
                if (gaussian > 3.2) gaussian = 3.2;
                if (gaussian < -3.2) gaussian = -3.2;
                double innovation = Math.Sqrt(Math.Max(0.0, 1.0 - rho * rho)) * sigma * gaussian;
                drift = drift * rho + innovation;

                value = center + drift;
                // A small right tail keeps the rhythm from feeling like a metronome.
                if (random.NextDouble() < 0.07)
                {
                    value += sigma * (0.45 + random.NextDouble() * 1.05);
                }
                if (value < min) value = min;
                if (value > max) value = max;
            }

            if (settings.MicroPauseChance > 0 && random.NextDouble() * 100.0 < settings.MicroPauseChance)
            {
                value += NextAround(random, settings.MicroPauseMs, 0.35);
            }
            if (settings.LongPauseChance > 0 && random.NextDouble() * 100.0 < settings.LongPauseChance)
            {
                value += NextAround(random, settings.LongPauseMs, 0.32);
            }

            int result = (int)Math.Round(value);
            return Math.Max(1, result);
        }

        private static int NextHold(Random random, AppSettings settings)
        {
            int min = Math.Min(settings.HoldMinMs, settings.HoldMaxMs);
            int max = Math.Max(settings.HoldMinMs, settings.HoldMaxMs);
            if (min < 1) min = 1;
            if (max < min) max = min;
            return random.Next(min, max + 1);
        }

        private static int NextAround(Random random, int center, double ratio)
        {
            if (center < 1) center = 1;
            double low = center * (1.0 - ratio);
            double high = center * (1.0 + ratio);
            int value = (int)Math.Round(low + random.NextDouble() * (high - low));
            return Math.Max(1, value);
        }

        private static double NextGaussian(Random random)
        {
            double u1 = 1.0 - random.NextDouble();
            double u2 = 1.0 - random.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }

        private static bool SendMouseDown(MouseButtonKind button)
        {
            switch (button)
            {
                case MouseButtonKind.Right:
                    return Native.SendMouse(Native.MOUSEEVENTF_RIGHTDOWN, 0, 0);
                case MouseButtonKind.Middle:
                    return Native.SendMouse(Native.MOUSEEVENTF_MIDDLEDOWN, 0, 0);
                default:
                    return Native.SendMouse(Native.MOUSEEVENTF_LEFTDOWN, 0, 0);
            }
        }

        private static bool SendMouseUp(MouseButtonKind button)
        {
            switch (button)
            {
                case MouseButtonKind.Right:
                    return Native.SendMouse(Native.MOUSEEVENTF_RIGHTUP, 0, 0);
                case MouseButtonKind.Middle:
                    return Native.SendMouse(Native.MOUSEEVENTF_MIDDLEUP, 0, 0);
                default:
                    return Native.SendMouse(Native.MOUSEEVENTF_LEFTUP, 0, 0);
            }
        }

        private bool WaitMs(int milliseconds)
        {
            if (milliseconds <= 0)
            {
                return !_stopRequested;
            }
            return WaitUntil(Stopwatch.GetTimestamp() + MsToTicks(milliseconds));
        }

        private static long MsToTicks(int milliseconds)
        {
            return (long)Math.Round(milliseconds * (Stopwatch.Frequency / 1000.0));
        }

        private bool WaitUntil(long targetTimestamp)
        {
            while (true)
            {
                if (_stopRequested)
                {
                    return false;
                }

                long remaining = targetTimestamp - Stopwatch.GetTimestamp();
                if (remaining <= 0)
                {
                    return true;
                }

                double remainingMs = remaining * 1000.0 / Stopwatch.Frequency;
                if (remainingMs > 6.0)
                {
                    int sleep = (int)Math.Floor(remainingMs - 3.0);
                    if (sleep > 50) sleep = 50;
                    if (sleep < 1) sleep = 1;
                    Thread.Sleep(sleep);
                }
                else if (remainingMs > 2.0)
                {
                    Thread.Sleep(1);
                }
                else
                {
                    Thread.SpinWait(160);
                }
            }
        }

        private void RaiseSeedReady(int seed)
        {
            try
            {
                EventHandler<int> handler = SeedReady;
                if (handler != null) handler(this, seed);
            }
            catch
            {
            }
        }

        private void RaiseCountdown(int remaining, int total)
        {
            try
            {
                EventHandler<CountdownEventArgs> handler = CountdownTick;
                if (handler != null) handler(this, new CountdownEventArgs(remaining, total));
            }
            catch
            {
            }
        }

        private void RaiseClicked(ClickSnapshot snapshot)
        {
            try
            {
                EventHandler<ClickSnapshot> handler = Clicked;
                if (handler != null) handler(this, snapshot);
            }
            catch
            {
            }
        }

        private void RaiseFinished(RunResult result)
        {
            try
            {
                EventHandler<RunResult> handler = Finished;
                if (handler != null) handler(this, result);
            }
            catch
            {
            }
        }
    }
}
