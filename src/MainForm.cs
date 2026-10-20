using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace NaturalClicker
{
    internal enum UiState
    {
        Idle,
        Counting,
        Running,
        Stopping
    }

    internal sealed class MainForm : Form
    {
        private const int HotkeyId = 0x4E43;
        private const string PresetCustom = "自定义配置";
        private const string PresetNatural = "自然节奏（推荐）";
        private const string PresetLight = "轻快办公";
        private const string PresetFast = "高速连点";
        private const string PresetTurbo = "极速稳定";
        private const string PresetSlow = "慢速稳定";

        private readonly ClickEngine _engine;
        private AppSettings _settings;
        private readonly Icon _appIcon;

        private TitleBar _titleBar;
        private HeroPanel _hero;
        private CardPanel _settingsCard;
        private CardPanel _actionCard;
        private Sparkline _sparkline;
        private StatusDot _statusDot;
        private Label _statusLabel;
        private Label _countLabel;
        private Label _elapsedLabel;
        private Label _avgLabel;
        private Label _intervalLabel;
        private Label _hintLabel;
        private Label _seedFooterLabel;
        private StartButton _startButton;

        private SelectField _mouseSelect;
        private SelectField _positionSelect;
        private SelectField _distributionSelect;
        private SelectField _hotkeySelect;
        private SelectField _presetSelect;
        private InputField _intervalMinField;
        private InputField _intervalMaxField;
        private InputField _holdMinField;
        private InputField _holdMaxField;
        private InputField _jitterField;
        private InputField _seedField;
        private InputField _countdownField;
        private InputField _maxClicksField;
        private InputField _smoothField;
        private InputField _microChanceField;
        private InputField _microMsField;
        private InputField _longChanceField;
        private InputField _longMsField;
        private InputField _xField;
        private InputField _yField;
        private AppButton _pickButton;
        private ToggleOption _minimizeToggle;
        private ToggleOption _topmostToggle;
        private AppButton _savePresetButton;
        private AppButton _resetButton;

        private CountdownOverlay _overlay;
        private NotifyIcon _tray;
        private ContextMenuStrip _trayMenu;
        private ToolStripMenuItem _trayToggleItem;

        private Timer _uiTimer;
        private Timer _hintTimer;
        private Timer _saveTimer;
        private Timer _pickTimer;
        private ToolTip _toolTip;

        private UiState _state = UiState.Idle;
        private bool _hasRun;
        private bool _picking;
        private int _pickRemaining;
        private long _clickCount;
        private DateTime _firstClickUtc = DateTime.MinValue;
        private int _lastSeed;
        private bool _hotkeyRegistered;
        private string _activeHotkey = "";
        private bool _suppressFieldEvents;
        private bool _suppressPresetEvent;
        private bool _closing;
        private List<PresetInfo> _presetInfos = new List<PresetInfo>();

        public MainForm()
        {
            Text = "自然连点器";
            FormBorderStyle = FormBorderStyle.None;
            ClientSize = new Size(980, 820);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Background;
            AutoScaleMode = AutoScaleMode.None;
            DoubleBuffered = true;
            KeyPreview = true;
            Font = Theme.Ui9;
            TryLoadIcon(out _appIcon);
            Icon = _appIcon;

            _settings = SettingsStore.LoadSettings();
            _engine = new ClickEngine();
            _engine.CountdownTick += OnEngineCountdown;
            _engine.Clicked += OnEngineClicked;
            _engine.Finished += OnEngineFinished;
            _engine.SeedReady += OnEngineSeedReady;

            BuildUi();
            BuildTimers();
            BuildTray();
            _overlay = new CountdownOverlay();

            RefreshPresetList(PresetCustom);
            ApplySettingsToFields(_settings);

            Shown += OnFormShown;
        }

        private static void TryLoadIcon(out Icon icon)
        {
            try
            {
                icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                icon = null;
            }
            if (icon == null)
            {
                icon = SystemIcons.Application;
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000;
                return cp;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width > 0 && Height > 0)
            {
                using (System.Drawing.Drawing2D.GraphicsPath path =
                    Theme.RoundedPath(new Rectangle(0, 0, Width, Height), 20))
                {
                    Region old = Region;
                    Region = new Region(path);
                    if (old != null) old.Dispose();
                }
            }
        }

        private void BuildUi()
        {
            Panel body = new Panel();
            body.BackColor = Theme.Background;
            body.Bounds = new Rectangle(0, 58, 980, 762);
            Controls.Add(body);

            _titleBar = new TitleBar();
            _titleBar.Dock = DockStyle.None;
            _titleBar.Bounds = new Rectangle(0, 0, 980, 58);
            _titleBar.IconImage = BuildTitleIcon(_appIcon);
            Controls.Add(_titleBar);

            BuildHero(body);
            BuildSettingsCard(body);
            BuildActionCard(body);

            Label footerLeft = NewLabel(
                "自然连点器 1.0   ·   本地运行，不联网   ·   参数自动保存",
                Theme.Ui8, Theme.MutedDark, new Rectangle(26, 736, 500, 18));
            body.Controls.Add(footerLeft);

            _seedFooterLabel = NewLabel(
                "上次随机种子 —",
                Theme.Ui8, Theme.MutedDark, new Rectangle(600, 736, 354, 18));
            _seedFooterLabel.TextAlign = ContentAlignment.MiddleRight;
            body.Controls.Add(_seedFooterLabel);

            _toolTip = new ToolTip();
            _toolTip.InitialDelay = 450;
            _toolTip.AutoPopDelay = 9000;
            _toolTip.SetToolTip(footerLeft, "配置文件：" + SettingsStore.SettingsPath);
            _toolTip.SetToolTip(_seedField, "0 = 每次自动生成；填入本次随机种子可复现同一节奏");
            _toolTip.SetToolTip(_jitterField, "仅固定坐标生效：在目标点附近随机偏移 ±N 像素");
            _toolTip.SetToolTip(_smoothField, "仅真人节奏生效：数值越高，节奏变化越连贯");
            _toolTip.SetToolTip(_microChanceField, "每次点击后按概率追加一次短停顿");
            _toolTip.SetToolTip(_longChanceField, "按概率追加一次较长休息，模拟换手或短暂走神");
            _toolTip.SetToolTip(_pickButton, "窗口会最小化，3 秒后记录鼠标当前位置");
            _toolTip.SetToolTip(_hotkeySelect, "全局生效；若被其他程序占用可换一个");
            _toolTip.SetToolTip(_startButton, "开始或停止；运行中按全局热键可随时停止");
        }

        private static Image BuildTitleIcon(Icon icon)
        {
            Bitmap bitmap = new Bitmap(28, 28);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                if (icon != null)
                {
                    g.DrawIcon(icon, new Rectangle(0, 0, 28, 28));
                }
            }
            return bitmap;
        }

        private void BuildHero(Panel body)
        {
            _hero = new HeroPanel();
            _hero.Bounds = new Rectangle(24, 20, 932, 140);
            body.Controls.Add(_hero);

            _statusDot = new StatusDot();
            _statusDot.Location = new Point(24, 23);
            _hero.Controls.Add(_statusDot);

            _statusLabel = NewLabel("待机  ·  按 " + _settings.Hotkey + " 开始", Theme.Ui95, Theme.TextSoft,
                new Rectangle(43, 19, 350, 22));
            _hero.Controls.Add(_statusLabel);

            _countLabel = new Label();
            _countLabel.Text = "0";
            _countLabel.Font = Theme.Num34;
            _countLabel.ForeColor = Theme.Text;
            _countLabel.BackColor = Color.Transparent;
            _countLabel.TextAlign = ContentAlignment.MiddleLeft;
            _countLabel.Bounds = new Rectangle(22, 35, 188, 60);
            _hero.Controls.Add(_countLabel);

            Label countUnit = NewLabel("次点击", Theme.Ui85, Theme.Muted,
                new Rectangle(24, 96, 80, 16));
            _hero.Controls.Add(countUnit);

            _elapsedLabel = NewLabel("用时 00:00", Theme.Ui9, Theme.Muted,
                new Rectangle(214, 45, 180, 18));
            _avgLabel = NewLabel("平均 —", Theme.Ui9, Theme.Muted,
                new Rectangle(214, 68, 180, 18));
            _intervalLabel = NewLabel("最近间隔 —", Theme.Ui9, Theme.Muted,
                new Rectangle(214, 91, 180, 18));
            _hero.Controls.Add(_elapsedLabel);
            _hero.Controls.Add(_avgLabel);
            _hero.Controls.Add(_intervalLabel);

            Panel divider = new Panel();
            divider.BackColor = Color.FromArgb(31, 45, 63);
            divider.Bounds = new Rectangle(408, 22, 1, 96);
            _hero.Controls.Add(divider);

            _sparkline = new Sparkline();
            _sparkline.Bounds = new Rectangle(430, 20, 254, 100);
            _hero.Controls.Add(_sparkline);

            _startButton = new StartButton();
            _startButton.Bounds = new Rectangle(714, 30, 192, 80);
            _startButton.HotkeyText = _settings.Hotkey;
            _startButton.Click += delegate { ToggleRun(); };
            _hero.Controls.Add(_startButton);
        }

        private void BuildSettingsCard(Panel body)
        {
            _settingsCard = new CardPanel();
            _settingsCard.Bounds = new Rectangle(24, 174, 932, 432);
            body.Controls.Add(_settingsCard);

            Label title = NewLabel("点击参数", Theme.Ui11Bold, Theme.Text,
                new Rectangle(22, 15, 200, 24));
            _settingsCard.Controls.Add(title);

            Label note = NewLabel("修改将在下次启动时生效", Theme.Ui8, Theme.MutedDark,
                new Rectangle(620, 18, 290, 18));
            note.TextAlign = ContentAlignment.MiddleRight;
            _settingsCard.Controls.Add(note);

            int[] xs = new int[] { 22, 247, 472, 697 };
            int[] ys = new int[] { 54, 116, 178, 240, 302 };

            _mouseSelect = CreateSelect(new string[] { "鼠标左键", "鼠标右键", "鼠标中键" });
            AddSelectField(_settingsCard, "鼠标按键", _mouseSelect, xs[0], ys[0], 211);
            _positionSelect = CreateSelect(new string[] { "跟随当前光标", "固定屏幕坐标" });
            AddSelectField(_settingsCard, "点击位置", _positionSelect, xs[1], ys[0], 211);
            _jitterField = CreateInput("0", "px", false, false);
            AddInputField(_settingsCard, "位置微抖动", _jitterField, xs[2], ys[0], 211);
            _seedField = CreateInput("0", "0=自动", false, false);
            AddInputField(_settingsCard, "随机种子", _seedField, xs[3], ys[0], 211);

            _intervalMinField = CreateInput("120", "ms", false, false);
            AddInputField(_settingsCard, "最短间隔", _intervalMinField, xs[0], ys[1], 211);
            _intervalMaxField = CreateInput("260", "ms", false, false);
            AddInputField(_settingsCard, "最长间隔", _intervalMaxField, xs[1], ys[1], 211);
            _distributionSelect = CreateSelect(new string[] { "真人节奏（推荐）", "均匀随机", "固定间隔" });
            AddSelectField(_settingsCard, "间隔分布", _distributionSelect, xs[2], ys[1], 211);
            _countdownField = CreateInput("3", "秒", false, false);
            AddInputField(_settingsCard, "开始倒计时", _countdownField, xs[3], ys[1], 211);

            _holdMinField = CreateInput("40", "ms", false, false);
            AddInputField(_settingsCard, "按下时长下限", _holdMinField, xs[0], ys[2], 211);
            _holdMaxField = CreateInput("90", "ms", false, false);
            AddInputField(_settingsCard, "按下时长上限", _holdMaxField, xs[1], ys[2], 211);
            _smoothField = CreateInput("55", "%", false, false);
            AddInputField(_settingsCard, "节奏平滑", _smoothField, xs[2], ys[2], 211);
            _maxClicksField = CreateInput("0", "0=不限", false, false);
            AddInputField(_settingsCard, "最大点击数", _maxClicksField, xs[3], ys[2], 211);

            _microChanceField = CreateInput("3", "%", true, false);
            AddInputField(_settingsCard, "微停顿概率", _microChanceField, xs[0], ys[3], 211);
            _microMsField = CreateInput("180", "ms", false, false);
            AddInputField(_settingsCard, "微停顿时长", _microMsField, xs[1], ys[3], 211);
            _longChanceField = CreateInput("0.25", "%", true, false);
            AddInputField(_settingsCard, "长休息概率", _longChanceField, xs[2], ys[3], 211);
            _longMsField = CreateInput("1600", "ms", false, false);
            AddInputField(_settingsCard, "长休息时长", _longMsField, xs[3], ys[3], 211);

            _xField = CreateInput("0", "px", false, true);
            AddInputField(_settingsCard, "X 坐标", _xField, xs[0], ys[4], 211);
            _yField = CreateInput("0", "px", false, true);
            AddInputField(_settingsCard, "Y 坐标", _yField, xs[1], ys[4], 211);

            Label pickLabel = NewLabel("固定坐标拾取", Theme.Ui8, Theme.Muted,
                new Rectangle(xs[2], ys[4], 211, 16));
            _settingsCard.Controls.Add(pickLabel);

            _pickButton = new AppButton();
            _pickButton.Text = "拾取屏幕位置（3 秒）";
            _pickButton.Bounds = new Rectangle(xs[2], ys[4] + 19, 211, 36);
            _pickButton.FillColor = Theme.SurfaceRaised;
            _pickButton.HoverColor = Color.FromArgb(31, 44, 60);
            _pickButton.TextColor = Theme.TextSoft;
            _pickButton.BorderColor = Theme.Border;
            _pickButton.Font = Theme.Ui85;
            _pickButton.Click += delegate { BeginPick(); };
            _settingsCard.Controls.Add(_pickButton);

            Label coordLabel = NewLabel("坐标说明", Theme.Ui8, Theme.Muted,
                new Rectangle(xs[3], ys[4], 211, 16));
            _settingsCard.Controls.Add(coordLabel);
            Label coordHint = NewLabel(
                "支持多屏，负坐标有效\n固定坐标可叠加位置微抖动\n启动后窗口仍可最小化",
                Theme.Ui8, Theme.MutedDark, new Rectangle(xs[3], ys[4] + 18, 211, 50));
            _settingsCard.Controls.Add(coordHint);

            Label algorithmHint = NewLabel(
                "拟人逻辑：在最短/最长间隔之间生成带惯性的平滑随机游走，并按概率插入微停顿与长休息；固定间隔模式会关闭额外停顿。",
                Theme.Ui8, Theme.MutedDark, new Rectangle(22, 386, 888, 20));
            _settingsCard.Controls.Add(algorithmHint);

            foreach (InputField field in AllInputFields())
            {
                field.ValueChanged += OnAnyFieldChanged;
            }
            _mouseSelect.SelectionChanged += OnAnySelectChanged;
            _positionSelect.SelectionChanged += OnPositionChanged;
            _distributionSelect.SelectionChanged += OnDistributionChanged;
        }

        private void BuildActionCard(Panel body)
        {
            _actionCard = new CardPanel();
            _actionCard.Bounds = new Rectangle(24, 620, 932, 98);
            body.Controls.Add(_actionCard);

            Label presetLabel = NewLabel("参数方案", Theme.Ui8, Theme.Muted,
                new Rectangle(22, 9, 180, 16));
            _actionCard.Controls.Add(presetLabel);
            _presetSelect = CreateSelect(new string[] { PresetCustom });
            _presetSelect.Bounds = new Rectangle(22, 29, 190, 38);
            _presetSelect.SelectionChanged += OnPresetChanged;
            _actionCard.Controls.Add(_presetSelect);

            Label hotkeyLabel = NewLabel("全局热键", Theme.Ui8, Theme.Muted,
                new Rectangle(226, 9, 120, 16));
            _actionCard.Controls.Add(hotkeyLabel);
            _hotkeySelect = CreateSelect(HotkeyCatalog.Names);
            _hotkeySelect.Bounds = new Rectangle(226, 29, 130, 38);
            _hotkeySelect.SelectionChanged += OnHotkeyChanged;
            _actionCard.Controls.Add(_hotkeySelect);

            _minimizeToggle = new ToggleOption();
            _minimizeToggle.Text = "启动后最小化";
            _minimizeToggle.Bounds = new Rectangle(372, 35, 168, 28);
            _minimizeToggle.CheckedChanged += OnToggleChanged;
            _actionCard.Controls.Add(_minimizeToggle);

            _topmostToggle = new ToggleOption();
            _topmostToggle.Text = "窗口置顶";
            _topmostToggle.Bounds = new Rectangle(552, 35, 140, 28);
            _topmostToggle.CheckedChanged += OnTopmostChanged;
            _actionCard.Controls.Add(_topmostToggle);

            _savePresetButton = new AppButton();
            _savePresetButton.Text = "保存方案";
            _savePresetButton.Bounds = new Rectangle(708, 29, 96, 38);
            _savePresetButton.FillColor = Theme.SurfaceRaised;
            _savePresetButton.HoverColor = Color.FromArgb(31, 44, 60);
            _savePresetButton.TextColor = Theme.TextSoft;
            _savePresetButton.BorderColor = Theme.Border;
            _savePresetButton.Font = Theme.Ui85;
            _savePresetButton.Click += delegate { SavePresetFlow(); };
            _actionCard.Controls.Add(_savePresetButton);

            _resetButton = new AppButton();
            _resetButton.Text = "恢复默认";
            _resetButton.Bounds = new Rectangle(812, 29, 98, 38);
            _resetButton.FillColor = Color.FromArgb(21, 29, 41);
            _resetButton.HoverColor = Color.FromArgb(29, 40, 55);
            _resetButton.TextColor = Theme.Muted;
            _resetButton.BorderColor = Theme.BorderSoft;
            _resetButton.Font = Theme.Ui85;
            _resetButton.Click += delegate { ResetDefaults(); };
            _actionCard.Controls.Add(_resetButton);

            _hintLabel = NewLabel("所有参数自动保存 · F6 可随时开始或停止",
                Theme.Ui8, Theme.MutedDark, new Rectangle(22, 75, 880, 17));
            _actionCard.Controls.Add(_hintLabel);
        }

        private List<InputField> AllInputFields()
        {
            return new List<InputField>
            {
                _intervalMinField, _intervalMaxField, _holdMinField, _holdMaxField,
                _jitterField, _seedField, _countdownField, _maxClicksField,
                _smoothField, _microChanceField, _microMsField,
                _longChanceField, _longMsField, _xField, _yField
            };
        }

        private static Label NewLabel(string text, Font font, Color color, Rectangle bounds)
        {
            Label label = new Label();
            label.Text = text;
            label.Font = font;
            label.ForeColor = color;
            label.BackColor = Color.Transparent;
            label.AutoSize = false;
            label.Bounds = bounds;
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        private static InputField CreateInput(string value, string suffix, bool allowDecimal, bool allowNegative)
        {
            InputField field = new InputField();
            field.Value = value;
            field.Suffix = suffix;
            field.AllowDecimal = allowDecimal;
            field.AllowNegative = allowNegative;
            return field;
        }

        private static SelectField CreateSelect(string[] items)
        {
            SelectField field = new SelectField();
            field.SetItems(items, 0);
            return field;
        }

        private static void AddInputField(Control parent, string label, InputField field, int x, int y, int width)
        {
            Label caption = NewLabel(label, Theme.Ui8, Theme.Muted, new Rectangle(x, y, width, 16));
            parent.Controls.Add(caption);
            field.Bounds = new Rectangle(x, y + 19, width, 36);
            parent.Controls.Add(field);
        }

        private static void AddSelectField(Control parent, string label, SelectField field, int x, int y, int width)
        {
            Label caption = NewLabel(label, Theme.Ui8, Theme.Muted, new Rectangle(x, y, width, 16));
            parent.Controls.Add(caption);
            field.Bounds = new Rectangle(x, y + 19, width, 36);
            parent.Controls.Add(field);
        }

        private void BuildTimers()
        {
            _uiTimer = new Timer();
            _uiTimer.Interval = 200;
            _uiTimer.Tick += OnUiTimerTick;

            _hintTimer = new Timer();
            _hintTimer.Interval = 5000;
            _hintTimer.Tick += delegate
            {
                _hintTimer.Stop();
                SetDefaultHint();
            };

            _saveTimer = new Timer();
            _saveTimer.Interval = 700;
            _saveTimer.Tick += delegate
            {
                _saveTimer.Stop();
                SaveCurrentSettingsSilently();
            };

            _pickTimer = new Timer();
            _pickTimer.Interval = 1000;
            _pickTimer.Tick += OnPickTimerTick;
        }

        private void BuildTray()
        {
            _trayMenu = new ContextMenuStrip();
            _trayMenu.Renderer = new DarkMenuRenderer();
            _trayMenu.BackColor = Theme.SurfaceRaised;
            _trayMenu.ForeColor = Theme.Text;
            _trayMenu.Font = Theme.Ui9;

            ToolStripMenuItem showItem = new ToolStripMenuItem("显示窗口");
            showItem.Click += delegate { RestoreWindow(); };
            _trayMenu.Items.Add(showItem);

            _trayToggleItem = new ToolStripMenuItem("开始连点");
            _trayToggleItem.Click += delegate { ToggleRun(); };
            _trayMenu.Items.Add(_trayToggleItem);

            _trayMenu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += delegate { Close(); };
            _trayMenu.Items.Add(exitItem);

            _tray = new NotifyIcon();
            _tray.Icon = _appIcon;
            _tray.Text = "自然连点器 · 待机";
            _tray.Visible = true;
            _tray.ContextMenuStrip = _trayMenu;
            _tray.DoubleClick += delegate { RestoreWindow(); };
            _tray.BalloonTipClicked += delegate { RestoreWindow(); };
            _trayMenu.Opening += delegate
            {
                bool active = _state == UiState.Counting || _state == UiState.Running;
                _trayToggleItem.Text = active ? "停止连点" : "开始连点";
            };
        }

        private void OnFormShown(object sender, EventArgs e)
        {
            RegisterHotkey(_settings.Hotkey, false);
            SetDefaultHint();
        }

        private void OnEngineSeedReady(object sender, int seed)
        {
            if (IsDisposed || _closing) return;
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(delegate { OnEngineSeedReady(null, seed); }));
                }
                catch { }
                return;
            }
            _lastSeed = seed;
            string prefix = _settings.Seed > 0 ? "固定种子 " : "本次随机种子 ";
            _seedFooterLabel.Text = prefix + seed + "  ·  可填入「随机种子」复现";
        }

        private void OnEngineCountdown(object sender, CountdownEventArgs e)
        {
            if (IsDisposed || _closing) return;
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(delegate { OnEngineCountdown(null, e); }));
                }
                catch { }
                return;
            }
            if (_state != UiState.Counting && _state != UiState.Running)
            {
                return;
            }

            if (e.Remaining > 0)
            {
                if (!_overlay.Visible)
                {
                    string subtitle = _positionSelect.SelectedIndex == 1
                        ? "固定坐标 " + _xField.Value + ", " + _yField.Value + "  ·  " + CurrentHotkey + " 取消"
                        : "跟随当前光标  ·  " + CurrentHotkey + " 取消";
                    _overlay.ShowCountdown("即将开始连点", subtitle, e.Total);
                }
                _overlay.SetCountdown(e.Remaining, e.Total);
                _statusLabel.Text = "倒计时 " + e.Remaining + " 秒  ·  准备开始";
                _statusDot.DotColor = Theme.Yellow;
                _statusDot.Active = true;
                _tray.Text = "自然连点器 · 倒计时 " + e.Remaining + " 秒";
            }
            else
            {
                _state = UiState.Running;
                _overlay.HideOverlay();
                _statusLabel.Text = "连点中  ·  按 " + CurrentHotkey + " 随时停止";
                _statusDot.DotColor = Theme.Green;
                _statusDot.Active = true;
                _startButton.Running = true;
                _startButton.Busy = false;
                _startButton.Enabled = true;
                _tray.Text = "自然连点器 · 连点中";
                UpdateDependentUi();
            }
        }

        private void OnEngineClicked(object sender, ClickSnapshot snapshot)
        {
            if (IsDisposed || _closing) return;
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(delegate { OnEngineClicked(null, snapshot); }));
                }
                catch { }
                return;
            }
            _clickCount = snapshot.Count;
            if (_firstClickUtc == DateTime.MinValue)
            {
                _firstClickUtc = DateTime.UtcNow.AddSeconds(-snapshot.ElapsedSeconds);
            }

            string countText = Theme.FormatCount(snapshot.Count);
            _countLabel.Text = countText;
            _countLabel.Font = countText.Length > 6 ? Theme.Num22 : Theme.Num34;
            _elapsedLabel.Text = "用时 " + Theme.FormatDuration(snapshot.ElapsedSeconds);
            _avgLabel.Text = snapshot.ClicksPerSecond > 0
                ? "平均 " + snapshot.ClicksPerSecond.ToString("0.0", CultureInfo.InvariantCulture) + " 次/秒"
                : "平均 —";
            _intervalLabel.Text = snapshot.LastIntervalMs > 0
                ? "最近间隔 " + Theme.FormatMs(snapshot.LastIntervalMs)
                : "最近间隔 —";
            _sparkline.SetData(snapshot.RecentIntervals);
            _tray.Text = "自然连点器 · 已点击 " + snapshot.Count + " 次";
        }

        private void OnEngineFinished(object sender, RunResult result)
        {
            if (IsDisposed || _closing) return;
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(delegate { OnEngineFinished(null, result); }));
                }
                catch { }
                return;
            }

            _state = UiState.Idle;
            _clickCount = result.Count;
            _startButton.Busy = false;
            _startButton.Running = false;
            _startButton.Enabled = true;
            _uiTimer.Stop();
            _overlay.HideOverlay();
            _statusDot.Active = false;

            string countText = Theme.FormatCount(result.Count);
            _countLabel.Text = countText;
            _countLabel.Font = countText.Length > 6 ? Theme.Num22 : Theme.Num34;

            if (result.Kind == RunEndKind.Completed)
            {
                _statusDot.DotColor = Theme.Green;
                _statusLabel.Text = "已完成  ·  共 " + countText + " 次点击";
                ShowHint("已达到设定点击数 · 按 " + CurrentHotkey + " 可再次开始", Theme.Green, 6000);
                if (WindowState == FormWindowState.Minimized)
                {
                    try
                    {
                        _tray.ShowBalloonTip(2500, "连点已完成",
                            "共完成 " + countText + " 次点击", ToolTipIcon.Info);
                    }
                    catch
                    {
                    }
                }
            }
            else if (result.Kind == RunEndKind.Blocked)
            {
                _statusDot.DotColor = Theme.Yellow;
                _statusLabel.Text = "已停止  ·  点击位置被窗口遮挡";
                ShowHint(result.Message, Theme.Yellow, 9000);
                Native.BeepWarning();
                RestoreWindow();
            }
            else if (result.Kind == RunEndKind.Error)
            {
                _statusDot.DotColor = Theme.Red;
                _statusLabel.Text = "已停止  ·  运行异常";
                ShowHint(result.Message, Theme.Red, 10000);
                Native.BeepWarning();
                RestoreWindow();
            }
            else
            {
                _statusDot.DotColor = Theme.Muted;
                _statusLabel.Text = result.Count > 0
                    ? "已停止  ·  共 " + countText + " 次点击"
                    : "已取消  ·  尚未开始点击";
                ShowHint(result.Count > 0
                    ? "已停止 · 按 " + CurrentHotkey + " 可再次开始"
                    : "已取消本次连点", Theme.Muted, 5000);
            }

            _tray.Text = "自然连点器 · 待机";
            UpdateDependentUi();
            if (_firstClickUtc != DateTime.MinValue && result.Count > 0)
            {
                double seconds = (DateTime.UtcNow - _firstClickUtc).TotalSeconds;
                if (seconds > 0)
                {
                    _elapsedLabel.Text = "用时 " + Theme.FormatDuration(seconds);
                }
            }
            _firstClickUtc = DateTime.MinValue;
        }

        private void OnUiTimerTick(object sender, EventArgs e)
        {
            _statusDot.Advance();
            if (_state == UiState.Running && _firstClickUtc != DateTime.MinValue)
            {
                _elapsedLabel.Text = "用时 " +
                    Theme.FormatDuration((DateTime.UtcNow - _firstClickUtc).TotalSeconds);
            }
        }

        private void ToggleRun()
        {
            if (_closing) return;
            if (_picking)
            {
                CancelPick();
                return;
            }
            if (_state == UiState.Idle)
            {
                BeginStart();
            }
            else if (_state == UiState.Counting || _state == UiState.Running)
            {
                StopRun();
            }
        }

        private void BeginStart()
        {
            AppSettings settings;
            string error;
            if (!TryReadSettings(out settings, out error))
            {
                ShowHint("参数有误：" + error, Theme.Red, 8000);
                Native.BeepWarning();
                return;
            }

            _settings = settings;
            try { SettingsStore.SaveSettings(_settings); } catch { }
            ResetStatsForStart();
            _hasRun = true;

            _state = UiState.Counting;
            _startButton.Running = true;
            _startButton.Busy = false;
            _startButton.Enabled = true;
            _startButton.HotkeyText = _settings.Hotkey;
            _statusLabel.Text = "准备开始  ·  即将进入倒计时";
            _statusDot.DotColor = Theme.Yellow;
            _statusDot.Active = true;
            _uiTimer.Start();
            UpdateDependentUi();
            _tray.Text = "自然连点器 · 准备中";

            if (_settings.MinimizeOnStart)
            {
                WindowState = FormWindowState.Minimized;
            }

            if (!_engine.Start(_settings, Handle))
            {
                _state = UiState.Idle;
                _uiTimer.Stop();
                _startButton.Running = false;
                _statusDot.Active = false;
                ShowHint("无法启动点击线程，请稍后重试。", Theme.Red, 6000);
                RestoreWindow();
            }
        }

        private void StopRun()
        {
            if (_state != UiState.Counting && _state != UiState.Running)
            {
                return;
            }
            _state = UiState.Stopping;
            _overlay.HideOverlay();
            _startButton.Busy = true;
            _startButton.Enabled = false;
            _statusDot.DotColor = Theme.Red;
            _statusDot.Active = true;
            _statusLabel.Text = "正在停止  ·  请稍候";
            _tray.Text = "自然连点器 · 正在停止";
            _engine.Stop();
        }

        private void ResetStatsForStart()
        {
            _clickCount = 0;
            _firstClickUtc = DateTime.MinValue;
            _countLabel.Text = "0";
            _countLabel.Font = Theme.Num34;
            _elapsedLabel.Text = "用时 00:00";
            _avgLabel.Text = "平均 —";
            _intervalLabel.Text = "最近间隔 —";
            _sparkline.Clear();
            _seedFooterLabel.Text = "本次随机种子 —";
        }

        private void BeginPick()
        {
            if (_state != UiState.Idle || _picking)
            {
                ShowHint("请先停止当前连点，再拾取坐标。", Theme.Yellow, 4000);
                return;
            }
            _picking = true;
            _pickRemaining = 3;
            _pickTimer.Start();
            _overlay.ShowCountdown("拾取点击位置", "将鼠标移到目标位置，3 秒后自动记录", 3);
            _overlay.SetCountdown(3, 3);
            if (WindowState != FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Minimized;
            }
            UpdateDependentUi();
        }

        private void OnPickTimerTick(object sender, EventArgs e)
        {
            if (!_picking)
            {
                _pickTimer.Stop();
                return;
            }
            _pickRemaining--;
            if (_pickRemaining > 0)
            {
                _overlay.SetCountdown(_pickRemaining, 3);
                return;
            }

            _pickTimer.Stop();
            Point point = Native.GetCursorPosition();
            _suppressFieldEvents = true;
            _xField.Value = point.X.ToString(CultureInfo.InvariantCulture);
            _yField.Value = point.Y.ToString(CultureInfo.InvariantCulture);
            _positionSelect.SelectSilently(1);
            _suppressFieldEvents = false;
            _picking = false;
            _overlay.HideOverlay();
            RestoreWindow();
            MarkCustom();
            UpdateDependentUi();
            ScheduleSave();
            ShowHint("已拾取坐标 (" + point.X + ", " + point.Y + ")", Theme.Green, 5000);
        }

        private void CancelPick()
        {
            _pickTimer.Stop();
            _picking = false;
            _overlay.HideOverlay();
            RestoreWindow();
            UpdateDependentUi();
            ShowHint("已取消坐标拾取", Theme.Muted, 3000);
        }

        private void OnAnyFieldChanged(object sender, EventArgs e)
        {
            if (_suppressFieldEvents) return;
            MarkCustom();
            ScheduleSave();
        }

        private void OnAnySelectChanged(object sender, EventArgs e)
        {
            if (_suppressFieldEvents) return;
            MarkCustom();
            ScheduleSave();
        }

        private void OnPositionChanged(object sender, EventArgs e)
        {
            if (_suppressFieldEvents) return;
            MarkCustom();
            UpdateDependentUi();
            ScheduleSave();
        }

        private void OnDistributionChanged(object sender, EventArgs e)
        {
            if (_suppressFieldEvents) return;
            MarkCustom();
            UpdateDependentUi();
            ScheduleSave();
        }

        private void OnHotkeyChanged(object sender, EventArgs e)
        {
            if (_suppressFieldEvents) return;
            string hotkey = _hotkeySelect.SelectedItem;
            if (string.IsNullOrEmpty(hotkey)) return;
            RegisterHotkey(hotkey, true);
            MarkCustom();
            ScheduleSave();
        }

        private void OnToggleChanged(object sender, EventArgs e)
        {
            if (_suppressFieldEvents) return;
            MarkCustom();
            ScheduleSave();
        }

        private void OnTopmostChanged(object sender, EventArgs e)
        {
            if (_suppressFieldEvents) return;
            TopMost = _topmostToggle.Checked;
            MarkCustom();
            ScheduleSave();
            ShowHint(TopMost ? "窗口已置顶" : "已取消窗口置顶", Theme.Muted, 2500);
        }

        private void OnPresetChanged(object sender, EventArgs e)
        {
            if (_suppressPresetEvent) return;
            int index = _presetSelect.SelectedIndex;
            if (index <= 0) return;

            if (index >= 1 && index <= 5)
            {
                AppSettings preset = null;
                string name = _presetSelect.SelectedItem;
                if (name == PresetNatural) preset = AppSettings.NaturalPreset();
                else if (name == PresetLight) preset = AppSettings.LightPreset();
                else if (name == PresetFast) preset = AppSettings.FastPreset();
                else if (name == PresetTurbo) preset = AppSettings.TurboPreset();
                else if (name == PresetSlow) preset = AppSettings.SlowPreset();
                if (preset == null) return;

                _settings.CopyTimingFrom(preset);
                ApplySettingsToFields(_settings);
                _suppressPresetEvent = true;
                _presetSelect.SelectByText(name);
                _suppressPresetEvent = false;
                ScheduleSave();
                ShowHint("已应用「" + name + "」的节奏参数，目标与热键保持不变", Theme.Green, 5000);
            }
            else
            {
                int customIndex = index - 6;
                if (customIndex < 0 || customIndex >= _presetInfos.Count) return;
                PresetInfo info = _presetInfos[customIndex];
                try
                {
                    AppSettings loaded = SettingsStore.LoadPreset(info.FilePath);
                    _settings = loaded;
                    ApplySettingsToFields(_settings);
                    RegisterHotkey(_settings.Hotkey, false);
                    _suppressPresetEvent = true;
                    _presetSelect.SelectByText("方案 · " + info.Name);
                    _suppressPresetEvent = false;
                    ScheduleSave();
                    ShowHint("已载入方案「" + info.Name + "」", Theme.Green, 4000);
                }
                catch
                {
                    ShowHint("方案载入失败，文件可能已损坏。", Theme.Red, 6000);
                    _suppressPresetEvent = true;
                    _presetSelect.SelectSilently(0);
                    _suppressPresetEvent = false;
                }
            }
        }

        private void MarkCustom()
        {
            if (_suppressPresetEvent) return;
            _suppressPresetEvent = true;
            _presetSelect.SelectSilently(0);
            _suppressPresetEvent = false;
        }

        private void SavePresetFlow()
        {
            AppSettings settings;
            string error;
            if (!TryReadSettings(out settings, out error))
            {
                ShowHint("参数有误：" + error, Theme.Red, 8000);
                return;
            }
            _settings = settings;

            using (PromptDialog dialog = new PromptDialog())
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                try
                {
                    PresetInfo info = SettingsStore.SavePreset(dialog.PresetName, settings);
                    RefreshPresetList("方案 · " + info.Name);
                    ShowHint("方案「" + info.Name + "」已保存", Theme.Green, 4000);
                    SettingsStore.SaveSettings(settings);
                }
                catch (Exception ex)
                {
                    ShowHint("保存方案失败：" + ex.Message, Theme.Red, 7000);
                }
            }
        }

        private void ResetDefaults()
        {
            _settings = new AppSettings();
            ApplySettingsToFields(_settings);
            RegisterHotkey(_settings.Hotkey, false);
            _suppressPresetEvent = true;
            _presetSelect.SelectByText(PresetNatural);
            _suppressPresetEvent = false;
            SettingsStore.SaveSettings(_settings);
            ShowHint("已恢复默认参数", Theme.Green, 4000);
        }

        private void RefreshPresetList(string selectLabel)
        {
            _presetInfos = SettingsStore.ListPresets();
            List<string> items = new List<string>();
            items.Add(PresetCustom);
            items.Add(PresetNatural);
            items.Add(PresetLight);
            items.Add(PresetFast);
            items.Add(PresetTurbo);
            items.Add(PresetSlow);
            foreach (PresetInfo info in _presetInfos)
            {
                items.Add("方案 · " + info.Name);
            }

            int index = 0;
            if (!string.IsNullOrEmpty(selectLabel))
            {
                int found = items.IndexOf(selectLabel);
                if (found >= 0) index = found;
            }
            _suppressPresetEvent = true;
            _presetSelect.SetItems(items.ToArray(), index);
            _suppressPresetEvent = false;
        }

        private void ApplySettingsToFields(AppSettings s)
        {
            _suppressFieldEvents = true;

            _mouseSelect.SelectSilently((int)s.Button);
            _positionSelect.SelectSilently(s.FixedPosition ? 1 : 0);
            _distributionSelect.SelectSilently((int)s.Distribution);

            _intervalMinField.Value = s.IntervalMinMs.ToString(CultureInfo.InvariantCulture);
            _intervalMaxField.Value = s.IntervalMaxMs.ToString(CultureInfo.InvariantCulture);
            _holdMinField.Value = s.HoldMinMs.ToString(CultureInfo.InvariantCulture);
            _holdMaxField.Value = s.HoldMaxMs.ToString(CultureInfo.InvariantCulture);
            _jitterField.Value = s.PositionJitter.ToString(CultureInfo.InvariantCulture);
            _seedField.Value = s.Seed.ToString(CultureInfo.InvariantCulture);
            _countdownField.Value = s.CountdownSeconds.ToString(CultureInfo.InvariantCulture);
            _maxClicksField.Value = s.MaxClicks.ToString(CultureInfo.InvariantCulture);
            _smoothField.Value = s.RhythmSmooth.ToString(CultureInfo.InvariantCulture);
            _microChanceField.Value = s.MicroPauseChance.ToString("0.##", CultureInfo.InvariantCulture);
            _microMsField.Value = s.MicroPauseMs.ToString(CultureInfo.InvariantCulture);
            _longChanceField.Value = s.LongPauseChance.ToString("0.##", CultureInfo.InvariantCulture);
            _longMsField.Value = s.LongPauseMs.ToString(CultureInfo.InvariantCulture);
            _xField.Value = s.FixedX.ToString(CultureInfo.InvariantCulture);
            _yField.Value = s.FixedY.ToString(CultureInfo.InvariantCulture);

            _minimizeToggle.Checked = s.MinimizeOnStart;
            _topmostToggle.Checked = s.AlwaysOnTop;
            TopMost = s.AlwaysOnTop;

            int hotkeyIndex = Array.IndexOf(HotkeyCatalog.Names, s.Hotkey);
            _hotkeySelect.SelectSilently(hotkeyIndex >= 0 ? hotkeyIndex : 0);
            _startButton.HotkeyText = s.Hotkey;

            _suppressFieldEvents = false;
            UpdateDependentUi();
        }

        private bool TryReadSettings(out AppSettings settings, out string error)
        {
            settings = _settings.Clone();
            error = "";

            int v;
            if (!ReadInt(_intervalMinField, "最短间隔", 5, 60000, out v, out error)) return false;
            settings.IntervalMinMs = v;
            if (!ReadInt(_intervalMaxField, "最长间隔", 5, 60000, out v, out error)) return false;
            settings.IntervalMaxMs = v;
            if (settings.IntervalMaxMs < settings.IntervalMinMs)
            {
                error = "「最长间隔」不能小于「最短间隔」";
                return false;
            }

            if (!ReadInt(_holdMinField, "按下时长下限", 1, 5000, out v, out error)) return false;
            settings.HoldMinMs = v;
            if (!ReadInt(_holdMaxField, "按下时长上限", 1, 5000, out v, out error)) return false;
            settings.HoldMaxMs = v;
            if (settings.HoldMaxMs < settings.HoldMinMs)
            {
                error = "「按下时长上限」不能小于下限";
                return false;
            }

            if (!ReadInt(_jitterField, "位置微抖动", 0, 100, out v, out error)) return false;
            settings.PositionJitter = v;
            if (!ReadInt(_seedField, "随机种子", 0, int.MaxValue, out v, out error)) return false;
            settings.Seed = v;
            if (!ReadInt(_countdownField, "开始倒计时", 1, 60, out v, out error)) return false;
            settings.CountdownSeconds = v;
            if (!ReadInt(_maxClicksField, "最大点击数", 0, 100000000, out v, out error)) return false;
            settings.MaxClicks = v;
            if (!ReadInt(_smoothField, "节奏平滑", 0, 100, out v, out error)) return false;
            settings.RhythmSmooth = v;

            double d;
            if (!ReadDouble(_microChanceField, "微停顿概率", 0, 100, out d, out error)) return false;
            settings.MicroPauseChance = d;
            if (!ReadInt(_microMsField, "微停顿时长", 10, 60000, out v, out error)) return false;
            settings.MicroPauseMs = v;
            if (!ReadDouble(_longChanceField, "长休息概率", 0, 100, out d, out error)) return false;
            settings.LongPauseChance = d;
            if (!ReadInt(_longMsField, "长休息时长", 10, 60000, out v, out error)) return false;
            settings.LongPauseMs = v;

            if (!ReadInt(_xField, "X 坐标", -32768, 32767, out v, out error)) return false;
            settings.FixedX = v;
            if (!ReadInt(_yField, "Y 坐标", -32768, 32767, out v, out error)) return false;
            settings.FixedY = v;

            settings.Button = (MouseButtonKind)Math.Max(0, Math.Min(2, _mouseSelect.SelectedIndex));
            settings.FixedPosition = _positionSelect.SelectedIndex == 1;
            settings.Distribution = (IntervalDistribution)Math.Max(0, Math.Min(2, _distributionSelect.SelectedIndex));
            settings.Hotkey = _hotkeySelect.SelectedItem;
            if (string.IsNullOrEmpty(settings.Hotkey))
            {
                settings.Hotkey = "F6";
            }
            settings.MinimizeOnStart = _minimizeToggle.Checked;
            settings.AlwaysOnTop = _topmostToggle.Checked;
            settings.Version = 1;

            if (settings.FixedPosition)
            {
                Rectangle vs = Native.GetVirtualScreen();
                if (settings.FixedX < vs.Left || settings.FixedX >= vs.Right ||
                    settings.FixedY < vs.Top || settings.FixedY >= vs.Bottom)
                {
                    error = "固定坐标超出当前屏幕范围（" + vs.Left + "," + vs.Top + " 至 " +
                            (vs.Right - 1) + "," + (vs.Bottom - 1) + "）";
                    return false;
                }
            }
            return true;
        }

        private static bool ReadInt(InputField field, string name, int min, int max,
            out int value, out string error)
        {
            error = "";
            string text = (field.Value ?? string.Empty).Trim();
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                error = "「" + name + "」需要填写整数";
                return false;
            }
            if (value < min || value > max)
            {
                error = "「" + name + "」的取值范围是 " + min + " – " + max;
                return false;
            }
            return true;
        }

        private static bool ReadDouble(InputField field, string name, double min, double max,
            out double value, out string error)
        {
            error = "";
            string text = (field.Value ?? string.Empty).Trim().Replace(',', '.');
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                error = "「" + name + "」需要填写数字";
                return false;
            }
            if (value < min || value > max)
            {
                error = "「" + name + "」的取值范围是 " + min + " – " + max;
                return false;
            }
            return true;
        }

        private string CurrentHotkey
        {
            get
            {
                if (_hotkeySelect != null && !string.IsNullOrEmpty(_hotkeySelect.SelectedItem))
                {
                    return _hotkeySelect.SelectedItem;
                }
                return _settings != null ? _settings.Hotkey : "F6";
            }
        }

        private void UpdateDependentUi()
        {
            bool fixedPosition = _positionSelect.SelectedIndex == 1;
            bool idle = _state == UiState.Idle;
            _xField.Enabled = fixedPosition && idle;
            _yField.Enabled = fixedPosition && idle;
            _pickButton.Enabled = idle && !_picking;
            _pickButton.Text = _picking ? "拾取中…" : "拾取屏幕位置（3 秒）";

            int distribution = _distributionSelect.SelectedIndex;
            bool human = distribution == 0;
            bool allowPauses = distribution != 2;
            _smoothField.Enabled = human;
            _microChanceField.Enabled = allowPauses;
            _microMsField.Enabled = allowPauses;
            _longChanceField.Enabled = allowPauses;
            _longMsField.Enabled = allowPauses;

            _startButton.HotkeyText = CurrentHotkey;
            if (_state == UiState.Idle && _clickCount == 0 && !_hasRun)
            {
                _statusLabel.Text = "待机  ·  按 " + CurrentHotkey + " 开始";
            }
        }

        private void ScheduleSave()
        {
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        private void SaveCurrentSettingsSilently()
        {
            AppSettings settings;
            string error;
            if (TryReadSettings(out settings, out error))
            {
                _settings = settings;
                try { SettingsStore.SaveSettings(settings); } catch { }
            }
        }

        private void SetDefaultHint()
        {
            if (_state == UiState.Counting || _state == UiState.Running || _state == UiState.Stopping)
            {
                _hintLabel.Text = "连点中 · 按 " + CurrentHotkey + " 停止 · 参数修改将在下次启动时生效";
                _hintLabel.ForeColor = Theme.MutedDark;
            }
            else
            {
                _hintLabel.Text = "所有参数自动保存 · " + CurrentHotkey + " 可随时开始或停止";
                _hintLabel.ForeColor = Theme.MutedDark;
            }
        }

        private void ShowHint(string text, Color color, int milliseconds)
        {
            _hintLabel.Text = text;
            _hintLabel.ForeColor = color;
            _hintTimer.Stop();
            _hintTimer.Interval = Math.Max(1000, milliseconds);
            _hintTimer.Start();
        }

        private void RegisterHotkey(string hotkey, bool announce)
        {
            uint modifiers;
            uint virtualKey;
            if (!HotkeyCatalog.TryParse(hotkey, out modifiers, out virtualKey))
            {
                ShowHint("无法识别热键：" + hotkey, Theme.Red, 5000);
                return;
            }
            if (!IsHandleCreated)
            {
                return;
            }

            string previous = _activeHotkey;
            if (_hotkeyRegistered)
            {
                Native.UnregisterHotKey(Handle, HotkeyId);
                _hotkeyRegistered = false;
            }

            if (Native.RegisterHotKey(Handle, HotkeyId, modifiers | Native.MOD_NOREPEAT, virtualKey))
            {
                _hotkeyRegistered = true;
                _activeHotkey = hotkey;
                _settings.Hotkey = hotkey;
                _startButton.HotkeyText = hotkey;
                UpdateDependentUi();
                if (announce)
                {
                    ShowHint("全局热键已更新为 " + hotkey, Theme.Green, 3000);
                }
                return;
            }

            bool restored = false;
            uint oldModifiers;
            uint oldKey;
            if (!string.IsNullOrEmpty(previous) &&
                HotkeyCatalog.TryParse(previous, out oldModifiers, out oldKey) &&
                Native.RegisterHotKey(Handle, HotkeyId, oldModifiers | Native.MOD_NOREPEAT, oldKey))
            {
                _hotkeyRegistered = true;
                _activeHotkey = previous;
                _settings.Hotkey = previous;
                _startButton.HotkeyText = previous;
                int oldIndex = Array.IndexOf(HotkeyCatalog.Names, previous);
                _suppressFieldEvents = true;
                _hotkeySelect.SelectSilently(oldIndex >= 0 ? oldIndex : 0);
                _suppressFieldEvents = false;
                restored = true;
            }

            if (restored)
            {
                ShowHint("热键 " + hotkey + " 被占用，已保留 " + previous, Theme.Red, 6000);
            }
            else
            {
                ShowHint("热键 " + hotkey + " 注册失败，可能被其他程序占用；仍可点击按钮开始。",
                    Theme.Red, 8000);
            }
            UpdateDependentUi();
        }

        private void RestoreWindow()
        {
            if (_closing || IsDisposed) return;
            if (!Visible) Show();
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }
            Activate();
            BringToFront();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_HOTKEY && m.WParam.ToInt32() == HotkeyId)
            {
                ToggleRun();
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape)
            {
                if (_picking) CancelPick();
                else if (_state == UiState.Counting || _state == UiState.Running) StopRun();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_closing)
            {
                base.OnFormClosing(e);
                return;
            }
            _closing = true;
            _pickTimer.Stop();
            _uiTimer.Stop();
            _hintTimer.Stop();
            _saveTimer.Stop();

            AppSettings settings;
            string error;
            if (TryReadSettings(out settings, out error))
            {
                _settings = settings;
                try { SettingsStore.SaveSettings(settings); } catch { }
            }

            try { _engine.StopAndWait(1500); } catch { }
            if (_hotkeyRegistered)
            {
                Native.UnregisterHotKey(Handle, HotkeyId);
                _hotkeyRegistered = false;
            }
            if (_overlay != null)
            {
                _overlay.HideOverlay();
                _overlay.Dispose();
            }
            if (_tray != null)
            {
                _tray.Visible = false;
                _tray.Dispose();
            }
            if (_trayMenu != null)
            {
                _trayMenu.Dispose();
            }
            if (_toolTip != null)
            {
                _toolTip.Dispose();
            }
            base.OnFormClosing(e);
        }
    }
}
