using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NaturalClicker
{
    internal class CardPanel : Panel
    {
        private int _radius = 18;
        private Color _cardColor = Theme.Surface;
        private Color _borderColor = Theme.BorderSoft;

        public int Radius
        {
            get { return _radius; }
            set { _radius = value; UpdateRegion(); Invalidate(); }
        }

        public Color CardColor
        {
            get { return _cardColor; }
            set { _cardColor = value; BackColor = value; Invalidate(); }
        }

        public Color BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; Invalidate(); }
        }

        public CardPanel()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            BackColor = _cardColor;
            TabStop = false;
            UpdateRegion();
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            UpdateRegion();
        }

        private void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0)
            {
                return;
            }
            using (GraphicsPath path = Theme.RoundedPath(new Rectangle(0, 0, Width, Height), _radius))
            {
                Region old = Region;
                Region = new Region(path);
                if (old != null) old.Dispose();
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(_cardColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = Theme.RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), _radius))
            using (Pen pen = new Pen(_borderColor, 1f))
            {
                e.Graphics.DrawPath(pen, path);
            }
            base.OnPaint(e);
        }
    }

    internal sealed class HeroPanel : CardPanel
    {
        public HeroPanel()
        {
            Radius = 20;
            BorderColor = Color.FromArgb(45, 61, 83);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width, Height);
            using (GraphicsPath path = Theme.RoundedPath(rect, Radius))
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    rect,
                    Color.FromArgb(25, 36, 52),
                    Color.FromArgb(14, 20, 30),
                    25f))
                {
                    g.FillPath(brush, path);
                }

                Rectangle glowRect = new Rectangle(Width - 460, -230, 500, 360);
                using (GraphicsPath glowPath = new GraphicsPath())
                {
                    glowPath.AddEllipse(glowRect);
                    using (PathGradientBrush glow = new PathGradientBrush(glowPath))
                    {
                        glow.CenterColor = Color.FromArgb(36, 73, 132, 255);
                        glow.SurroundColors = new Color[] { Color.FromArgb(0, 73, 132, 255) };
                        g.FillPath(glow, glowPath);
                    }
                }

                using (LinearGradientBrush lineBrush = new LinearGradientBrush(
                    new Rectangle(24, 0, Math.Max(1, Width - 48), 2),
                    Color.FromArgb(0, 120, 172, 255),
                    Color.FromArgb(145, 120, 172, 255),
                    0f))
                using (Pen topLine = new Pen(lineBrush, 1.4f))
                {
                    g.DrawLine(topLine, 28, 1, Width - 28, 1);
                }
            }
        }
    }

    internal class AppButton : Button
    {
        private bool _hover;
        private bool _pressed;
        private Color _fillColor = Theme.SurfaceRaised;
        private Color _hoverColor = Color.FromArgb(31, 43, 59);
        private Color _textColor = Theme.TextSoft;
        private Color _borderColor = Theme.Border;
        private int _cornerRadius = 9;

        public Color FillColor
        {
            get { return _fillColor; }
            set { _fillColor = value; Invalidate(); }
        }

        public Color HoverColor
        {
            get { return _hoverColor; }
            set { _hoverColor = value; Invalidate(); }
        }

        public Color TextColor
        {
            get { return _textColor; }
            set { _textColor = value; Invalidate(); }
        }

        public Color BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; Invalidate(); }
        }

        public int CornerRadius
        {
            get { return _cornerRadius; }
            set { _cornerRadius = value; UpdateRegion(); Invalidate(); }
        }

        public AppButton()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = _fillColor;
            ForeColor = _textColor;
            Font = Theme.Ui9Bold;
            Cursor = Cursors.Hand;
            TabStop = false;
            UseMnemonic = false;
            UpdateRegion();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateRegion();
        }

        private void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0)
            {
                return;
            }
            using (GraphicsPath path = Theme.RoundedPath(new Rectangle(0, 0, Width, Height), _cornerRadius))
            {
                Region old = Region;
                Region = new Region(path);
                if (old != null) old.Dispose();
            }
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color fill = Enabled ? _fillColor : Color.FromArgb(22, 29, 40);
            if (Enabled && _pressed)
            {
                fill = Theme.Blend(_hoverColor, Color.Black, 0.12);
            }
            else if (Enabled && _hover)
            {
                fill = _hoverColor;
            }

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = Theme.RoundedPath(rect, _cornerRadius))
            using (SolidBrush brush = new SolidBrush(fill))
            {
                g.FillPath(brush, path);
                if (_borderColor.A > 0)
                {
                    using (Pen pen = new Pen(Enabled ? _borderColor : Theme.BorderSoft, 1f))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }

            Color textColor = Enabled ? _textColor : Theme.MutedDark;
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding |
                TextFormatFlags.EndEllipsis);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            _pressed = true;
            Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
    }

    internal sealed class StartButton : Control
    {
        private bool _hover;
        private bool _pressed;
        private bool _running;
        private bool _busy;
        private string _hotkeyText = "F6";

        public bool Running
        {
            get { return _running; }
            set { _running = value; Invalidate(); }
        }

        public bool Busy
        {
            get { return _busy; }
            set { _busy = value; Invalidate(); }
        }

        public string HotkeyText
        {
            get { return _hotkeyText; }
            set { _hotkeyText = value; Invalidate(); }
        }

        public StartButton()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            TabStop = false;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width > 0 && Height > 0)
            {
                using (GraphicsPath path = Theme.RoundedPath(new Rectangle(0, 0, Width, Height), 15))
                {
                    Region old = Region;
                    Region = new Region(path);
                    if (old != null) old.Dispose();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            Color top;
            Color bottom;
            if (!Enabled)
            {
                top = Color.FromArgb(39, 48, 61);
                bottom = Color.FromArgb(30, 38, 50);
            }
            else if (_running)
            {
                top = _hover ? Color.FromArgb(242, 92, 108) : Color.FromArgb(224, 73, 91);
                bottom = _hover ? Color.FromArgb(202, 45, 64) : Color.FromArgb(183, 40, 58);
            }
            else
            {
                top = _hover ? Color.FromArgb(101, 162, 255) : Color.FromArgb(79, 143, 255);
                bottom = _hover ? Color.FromArgb(61, 116, 241) : Color.FromArgb(48, 97, 222);
            }
            if (_pressed && Enabled)
            {
                top = Theme.Blend(top, Color.Black, 0.1);
                bottom = Theme.Blend(bottom, Color.Black, 0.12);
            }

            using (GraphicsPath path = Theme.RoundedPath(rect, 15))
            using (LinearGradientBrush brush = new LinearGradientBrush(rect, top, bottom, 90f))
            {
                g.FillPath(brush, path);
                using (Pen inner = new Pen(Color.FromArgb(85, 255, 255, 255), 1f))
                {
                    Rectangle innerRect = new Rectangle(1, 1, Width - 3, Height - 3);
                    using (GraphicsPath innerPath = Theme.RoundedPath(innerRect, 14))
                    {
                        g.DrawPath(inner, innerPath);
                    }
                }
            }

            int centerY = Height / 2;
            int iconX = 27;
            using (SolidBrush iconBrush = new SolidBrush(Enabled ? Color.White : Theme.Muted))
            {
                if (_running)
                {
                    Rectangle stop = new Rectangle(iconX - 7, centerY - 8, 15, 15);
                    using (GraphicsPath path = Theme.RoundedPath(stop, 3))
                    {
                        g.FillPath(iconBrush, path);
                    }
                }
                else
                {
                    Point[] triangle = new Point[]
                    {
                        new Point(iconX - 6, centerY - 10),
                        new Point(iconX - 6, centerY + 10),
                        new Point(iconX + 10, centerY)
                    };
                    g.FillPolygon(iconBrush, triangle);
                }
            }

            string main = _busy ? "正在停止…" : (_running ? "停止连点" : "开始连点");
            string sub = _busy ? "稍候" : (_running ? _hotkeyText + " 立即停止" : _hotkeyText + " 开始 / 停止");
            Rectangle mainRect = new Rectangle(50, 13, Width - 62, 30);
            Rectangle subRect = new Rectangle(50, 45, Width - 62, 18);
            TextRenderer.DrawText(g, main, Theme.Ui13Bold, mainRect,
                Enabled ? Color.White : Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, sub, Theme.Ui8, subRect,
                Enabled ? Color.FromArgb(220, 234, 252) : Theme.MutedDark,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding |
                TextFormatFlags.EndEllipsis);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _pressed = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
    }

    internal sealed class CaptionButton : Control
    {
        private bool _hover;
        private string _glyph = "x";
        private bool _danger;

        public string Glyph
        {
            get { return _glyph; }
            set { _glyph = value; Invalidate(); }
        }

        public bool Danger
        {
            get { return _danger; }
            set { _danger = value; Invalidate(); }
        }

        public CaptionButton()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Theme.Background;
            Cursor = Cursors.Hand;
            TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_hover)
            {
                Color hoverFill = _danger ? Color.FromArgb(196, 55, 72) : Color.FromArgb(31, 43, 59);
                Theme.FillRounded(g, new Rectangle(0, 0, Width, Height), 7, hoverFill);
            }

            using (Pen pen = new Pen(_hover ? Color.White : Theme.Muted, 1.4f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                int cx = Width / 2;
                int cy = Height / 2;
                if (_glyph == "-")
                {
                    g.DrawLine(pen, cx - 6, cy, cx + 6, cy);
                }
                else
                {
                    g.DrawLine(pen, cx - 5, cy - 5, cx + 5, cy + 5);
                    g.DrawLine(pen, cx + 5, cy - 5, cx - 5, cy + 5);
                }
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }
    }

    internal sealed class TitleBar : Panel
    {
        private readonly CaptionButton _minimizeButton;
        private readonly CaptionButton _closeButton;
        private Image _iconImage;

        public Image IconImage
        {
            get { return _iconImage; }
            set { _iconImage = value; Invalidate(); }
        }

        public TitleBar()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Background;
            Height = 58;
            Dock = DockStyle.Top;

            _minimizeButton = new CaptionButton();
            _minimizeButton.Glyph = "-";
            _minimizeButton.Size = new Size(34, 28);
            _minimizeButton.Click += delegate
            {
                Form form = FindForm();
                if (form != null) form.WindowState = FormWindowState.Minimized;
            };
            Controls.Add(_minimizeButton);

            _closeButton = new CaptionButton();
            _closeButton.Glyph = "x";
            _closeButton.Danger = true;
            _closeButton.Size = new Size(34, 28);
            _closeButton.Click += delegate
            {
                Form form = FindForm();
                if (form != null) form.Close();
            };
            Controls.Add(_closeButton);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            if (_closeButton != null)
            {
                _closeButton.Location = new Point(Width - 48, 15);
            }
            if (_minimizeButton != null)
            {
                _minimizeButton.Location = new Point(Width - 88, 15);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_iconImage != null)
            {
                g.DrawImage(_iconImage, new Rectangle(24, 15, 28, 28));
            }
            else
            {
                Theme.FillRounded(g, new Rectangle(24, 15, 28, 28), 8, Theme.Accent);
            }

            TextRenderer.DrawText(g, "自然连点器", Theme.Ui12Bold,
                new Rectangle(63, 10, 320, 24), Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, "拟人节奏  ·  伪随机点击间隔", Theme.Ui8,
                new Rectangle(64, 32, 360, 18), Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

            using (Pen pen = new Pen(Theme.BorderSoft, 1f))
            {
                g.DrawLine(pen, 24, Height - 1, Width - 24, Height - 1);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                Form form = FindForm();
                if (form != null) Native.DragWindow(form.Handle);
            }
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            base.OnDoubleClick(e);
            Form form = FindForm();
            if (form != null) form.WindowState = FormWindowState.Minimized;
        }
    }

    internal sealed class InputField : Panel
    {
        private readonly TextBox _box;
        private string _suffix = "";
        private bool _focused;
        private bool _allowDecimal;
        private bool _allowNegative;
        private bool _suppressValueEvent;

        public event EventHandler ValueChanged;

        public string Value
        {
            get { return _box.Text; }
            set { _box.Text = value ?? string.Empty; }
        }

        public string Suffix
        {
            get { return _suffix; }
            set { _suffix = value ?? string.Empty; UpdateLayout(); Invalidate(); }
        }

        public bool AllowDecimal
        {
            get { return _allowDecimal; }
            set { _allowDecimal = value; }
        }

        public bool AllowNegative
        {
            get { return _allowNegative; }
            set { _allowNegative = value; }
        }

        public TextBox InnerTextBox
        {
            get { return _box; }
        }

        public InputField()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Input;
            Height = 36;
            Cursor = Cursors.IBeam;

            _box = new TextBox();
            _box.BorderStyle = BorderStyle.None;
            _box.BackColor = Theme.Input;
            _box.ForeColor = Theme.Text;
            _box.Font = Theme.Ui10;
            _box.ShortcutsEnabled = true;
            _box.AutoSize = false;
            _box.Height = 22;
            _box.Location = new Point(11, 7);
            _box.TextChanged += delegate
            {
                if (!_suppressValueEvent && ValueChanged != null)
                {
                    ValueChanged(this, EventArgs.Empty);
                }
            };
            _box.GotFocus += delegate { _focused = true; Invalidate(); };
            _box.LostFocus += delegate { _focused = false; Invalidate(); };
            _box.KeyPress += OnBoxKeyPress;
            Controls.Add(_box);

            Click += delegate { _box.Focus(); };
            UpdateRegion();
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            UpdateLayout();
            UpdateRegion();
        }

        private void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0)
            {
                return;
            }
            using (GraphicsPath path = Theme.RoundedPath(new Rectangle(0, 0, Width, Height), 9))
            {
                Region old = Region;
                Region = new Region(path);
                if (old != null) old.Dispose();
            }
        }

        private void UpdateLayout()
        {
            if (_box == null) return;
            int suffixWidth = string.IsNullOrEmpty(_suffix)
                ? 0
                : TextRenderer.MeasureText(_suffix, Theme.Ui8).Width + 8;
            _box.Width = Math.Max(10, Width - 22 - suffixWidth);
            _box.Location = new Point(11, Math.Max(6, (Height - _box.Height) / 2));
        }

        private void OnBoxKeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
            {
                return;
            }
            if (char.IsDigit(e.KeyChar))
            {
                return;
            }
            if (_allowDecimal && (e.KeyChar == '.' || e.KeyChar == ',') &&
                _box.Text.IndexOf('.') < 0 && _box.Text.IndexOf(',') < 0)
            {
                return;
            }
            if (_allowNegative && e.KeyChar == '-' && _box.SelectionStart == 0 &&
                _box.Text.IndexOf('-') < 0)
            {
                return;
            }
            e.Handled = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            Color fill = Enabled ? Theme.Input : Color.FromArgb(16, 22, 31);
            using (GraphicsPath path = Theme.RoundedPath(rect, 9))
            using (SolidBrush brush = new SolidBrush(fill))
            {
                g.FillPath(brush, path);
                using (Pen pen = new Pen(!Enabled ? Theme.BorderSoft : (_focused ? Theme.Accent : Theme.Border), 1f))
                {
                    g.DrawPath(pen, path);
                }
            }

            if (!string.IsNullOrEmpty(_suffix))
            {
                int suffixWidth = TextRenderer.MeasureText(_suffix, Theme.Ui8).Width + 8;
                Rectangle suffixRect = new Rectangle(Width - suffixWidth - 10, 0, suffixWidth, Height);
                TextRenderer.DrawText(g, _suffix, Theme.Ui8, suffixRect,
                    Enabled ? Theme.MutedDark : Color.FromArgb(74, 88, 105),
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (Enabled) _box.Focus();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            if (_box != null)
            {
                _box.Enabled = Enabled;
                _box.BackColor = Enabled ? Theme.Input : Color.FromArgb(16, 22, 31);
                _box.ForeColor = Enabled ? Theme.Text : Theme.MutedDark;
            }
            Invalidate();
        }

        public void SetValueSilent(string value)
        {
            _suppressValueEvent = true;
            _box.Text = value ?? string.Empty;
            _suppressValueEvent = false;
        }

        public void FocusInput()
        {
            if (Enabled) _box.Focus();
        }
    }

    internal sealed class SelectField : Control
    {
        private readonly List<string> _items = new List<string>();
        private int _selectedIndex = -1;
        private bool _hover;

        public event EventHandler SelectionChanged;

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set { SetSelectedIndex(value, true); }
        }

        public string SelectedItem
        {
            get
            {
                if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
                {
                    return _items[_selectedIndex];
                }
                return string.Empty;
            }
        }

        public SelectField()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Input;
            Height = 36;
            Cursor = Cursors.Hand;
            Font = Theme.Ui10;
            TabStop = false;
            UpdateRegion();
        }

        public void SetItems(string[] items, int selectedIndex)
        {
            _items.Clear();
            if (items != null) _items.AddRange(items);
            _selectedIndex = ClampIndex(selectedIndex);
            Invalidate();
        }

        public void SelectSilently(int index)
        {
            SetSelectedIndex(index, false);
        }

        public void SelectByText(string text)
        {
            int index = _items.IndexOf(text);
            if (index >= 0) SelectSilently(index);
        }

        public bool ContainsItem(string text)
        {
            return _items.IndexOf(text) >= 0;
        }

        private void SetSelectedIndex(int index, bool raiseEvent)
        {
            index = ClampIndex(index);
            if (index == _selectedIndex)
            {
                Invalidate();
                return;
            }
            _selectedIndex = index;
            Invalidate();
            if (raiseEvent && SelectionChanged != null)
            {
                SelectionChanged(this, EventArgs.Empty);
            }
        }

        private int ClampIndex(int index)
        {
            if (_items.Count == 0) return -1;
            if (index < 0) return 0;
            if (index >= _items.Count) return _items.Count - 1;
            return index;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateRegion();
        }

        private void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0) return;
            using (GraphicsPath path = Theme.RoundedPath(new Rectangle(0, 0, Width, Height), 9))
            {
                Region old = Region;
                Region = new Region(path);
                if (old != null) old.Dispose();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            Color fill = Enabled ? Theme.Input : Color.FromArgb(16, 22, 31);
            using (GraphicsPath path = Theme.RoundedPath(rect, 9))
            using (SolidBrush brush = new SolidBrush(fill))
            {
                g.FillPath(brush, path);
                using (Pen pen = new Pen(!Enabled ? Theme.BorderSoft : (_hover ? Theme.Border : Theme.Border), 1f))
                {
                    g.DrawPath(pen, path);
                }
            }

            Rectangle textRect = new Rectangle(11, 0, Width - 40, Height);
            TextRenderer.DrawText(g, SelectedItem, Font, textRect,
                Enabled ? Theme.Text : Theme.MutedDark,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding |
                TextFormatFlags.EndEllipsis);

            int cx = Width - 19;
            int cy = Height / 2;
            using (Pen pen = new Pen(Enabled ? Theme.Muted : Theme.MutedDark, 1.3f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawLine(pen, cx - 4, cy - 2, cx, cy + 2);
                g.DrawLine(pen, cx, cy + 2, cx + 4, cy - 2);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!Enabled || e.Button != MouseButtons.Left)
            {
                return;
            }
            ShowMenu();
        }

        private void ShowMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Font = Theme.Ui9;
            menu.BackColor = Theme.SurfaceRaised;
            menu.ForeColor = Theme.Text;
            menu.ShowImageMargin = false;
            menu.ShowCheckMargin = false;
            menu.Renderer = new DarkMenuRenderer();
            menu.MinimumSize = new Size(Math.Max(Width, 130), 0);
            menu.Padding = new Padding(3);

            for (int i = 0; i < _items.Count; i++)
            {
                ToolStripMenuItem item = new ToolStripMenuItem(_items[i]);
                item.Tag = i;
                item.Padding = new Padding(4, 2, 4, 2);
                item.Click += OnMenuItemClick;
                if (i == _selectedIndex)
                {
                    item.Font = Theme.Ui9Bold;
                }
                menu.Items.Add(item);
            }
            menu.Closed += delegate { menu.Dispose(); };
            menu.Show(this, new Point(0, Height + 5));
        }

        private void OnMenuItemClick(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            if (item == null) return;
            int index = (int)item.Tag;
            SelectedIndex = index;
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
    }

    internal sealed class ToggleOption : Control
    {
        private bool _checked;
        private bool _hover;

        public event EventHandler CheckedChanged;

        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value) return;
                _checked = value;
                Invalidate();
                if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
            }
        }

        public ToggleOption()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Height = 28;
            Font = Theme.Ui9;
            Cursor = Cursors.Hand;
            TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color textColor = !Enabled ? Theme.MutedDark : (_hover ? Theme.Text : Theme.TextSoft);
            Rectangle textRect = new Rectangle(0, 0, Width - 48, Height);
            TextRenderer.DrawText(g, Text, Font, textRect, textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding |
                TextFormatFlags.EndEllipsis);

            int pillWidth = 36;
            int pillHeight = 20;
            int px = Width - pillWidth;
            int py = (Height - pillHeight) / 2;
            Rectangle pill = new Rectangle(px, py, pillWidth, pillHeight);
            Color track = _checked
                ? (Enabled ? Theme.Accent : Color.FromArgb(53, 77, 111))
                : Color.FromArgb(46, 59, 76);
            Theme.FillRounded(g, pill, pillHeight / 2, track);
            if (!_checked)
            {
                using (Pen pen = new Pen(Color.FromArgb(67, 83, 103), 1f))
                {
                    using (GraphicsPath path = Theme.RoundedPath(pill, pillHeight / 2))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }

            int knobSize = 14;
            int knobX = _checked ? pill.Right - knobSize - 3 : pill.Left + 3;
            int knobY = pill.Top + (pillHeight - knobSize) / 2;
            using (SolidBrush knob = new SolidBrush(Enabled ? Color.White : Theme.Muted))
            {
                g.FillEllipse(knob, knobX, knobY, knobSize, knobSize);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (Enabled && e.Button == MouseButtons.Left)
            {
                Checked = !Checked;
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
    }

    internal sealed class StatusDot : Control
    {
        private Color _dotColor = Theme.MutedDark;
        private bool _active;
        private float _phase;

        public Color DotColor
        {
            get { return _dotColor; }
            set { _dotColor = value; Invalidate(); }
        }

        public bool Active
        {
            get { return _active; }
            set { _active = value; Invalidate(); }
        }

        public StatusDot()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(14, 14);
        }

        public void Advance()
        {
            _phase += 0.18f;
            if (_phase > 1f) _phase -= 1f;
            if (_active) Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int cx = Width / 2;
            int cy = Height / 2;
            if (_active)
            {
                int alpha = (int)(36 + 34 * (1.0 - _phase));
                using (SolidBrush glow = new SolidBrush(Color.FromArgb(alpha, _dotColor)))
                {
                    g.FillEllipse(glow, cx - 7, cy - 7, 14, 14);
                }
            }
            using (SolidBrush dot = new SolidBrush(_dotColor))
            {
                g.FillEllipse(dot, cx - 3, cy - 3, 6, 6);
            }
        }
    }

    internal sealed class Sparkline : Control
    {
        private double[] _values = new double[0];

        public Sparkline()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        public void SetData(double[] values)
        {
            _values = values ?? new double[0];
            Invalidate();
        }

        public void Clear()
        {
            _values = new double[0];
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            TextRenderer.DrawText(g, "节奏波形", Theme.Ui8,
                new Rectangle(0, 0, 100, 17), Theme.MutedDark,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

            string rightText = _values.Length > 0
                ? Theme.FormatMs(_values[_values.Length - 1])
                : "等待数据";
            TextRenderer.DrawText(g, rightText, Theme.Ui85,
                new Rectangle(Width - 100, 0, 100, 17),
                _values.Length > 0 ? Theme.Accent : Theme.MutedDark,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

            Rectangle plot = new Rectangle(1, 24, Width - 2, Height - 28);
            if (plot.Width < 10 || plot.Height < 8) return;

            using (Pen gridPen = new Pen(Color.FromArgb(32, 71, 91, 115), 1f))
            {
                gridPen.DashStyle = DashStyle.Dot;
                g.DrawLine(gridPen, plot.Left, plot.Top + plot.Height / 3,
                    plot.Right, plot.Top + plot.Height / 3);
                g.DrawLine(gridPen, plot.Left, plot.Top + plot.Height * 2 / 3,
                    plot.Right, plot.Top + plot.Height * 2 / 3);
            }

            if (_values.Length == 0)
            {
                using (Pen baseline = new Pen(Color.FromArgb(48, 73, 96, 122), 1f))
                {
                    baseline.DashStyle = DashStyle.Dash;
                    g.DrawLine(baseline, plot.Left, plot.Bottom - 1, plot.Right, plot.Bottom - 1);
                }
                TextRenderer.DrawText(g, "开始后显示实际点击间隔", Theme.Ui85,
                    plot, Theme.MutedDark,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
                return;
            }

            double min = double.MaxValue;
            double max = double.MinValue;
            for (int i = 0; i < _values.Length; i++)
            {
                if (_values[i] < min) min = _values[i];
                if (_values[i] > max) max = _values[i];
            }
            if (max - min < 20)
            {
                max += 10;
                min -= 10;
            }
            else
            {
                double pad = (max - min) * 0.12;
                max += pad;
                min -= pad;
            }
            if (min < 1) min = 1;

            PointF[] points = new PointF[_values.Length];
            for (int i = 0; i < _values.Length; i++)
            {
                float x = _values.Length == 1
                    ? plot.Right - 2
                    : plot.Left + (float)i * plot.Width / (_values.Length - 1);
                float ratio = (float)((_values[i] - min) / (max - min));
                float y = plot.Bottom - ratio * plot.Height;
                points[i] = new PointF(x, y);
            }

            if (points.Length >= 2)
            {
                using (GraphicsPath area = new GraphicsPath())
                {
                    area.AddLines(points);
                    area.AddLine(points[points.Length - 1].X, plot.Bottom, points[0].X, plot.Bottom);
                    area.CloseFigure();
                    using (LinearGradientBrush fill = new LinearGradientBrush(
                        plot,
                        Color.FromArgb(72, Theme.Accent),
                        Color.FromArgb(0, Theme.Accent),
                        90f))
                    {
                        g.FillPath(fill, area);
                    }
                }

                using (Pen line = new Pen(Theme.Accent, 1.7f))
                {
                    line.StartCap = LineCap.Round;
                    line.EndCap = LineCap.Round;
                    line.LineJoin = LineJoin.Round;
                    g.DrawLines(line, points);
                }
            }

            PointF last = points[points.Length - 1];
            using (SolidBrush halo = new SolidBrush(Color.FromArgb(50, Theme.Accent)))
            {
                g.FillEllipse(halo, last.X - 5, last.Y - 5, 10, 10);
            }
            using (SolidBrush dot = new SolidBrush(Theme.Accent))
            {
                g.FillEllipse(dot, last.X - 2.2f, last.Y - 2.2f, 4.4f, 4.4f);
            }
        }
    }

    internal sealed class DarkColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Theme.SurfaceRaised; } }
        public override Color ImageMarginGradientBegin { get { return Theme.SurfaceRaised; } }
        public override Color ImageMarginGradientMiddle { get { return Theme.SurfaceRaised; } }
        public override Color ImageMarginGradientEnd { get { return Theme.SurfaceRaised; } }
        public override Color MenuBorder { get { return Theme.Border; } }
        public override Color MenuItemBorder { get { return Theme.Accent; } }
        public override Color MenuItemSelected { get { return Theme.AccentDeep; } }
        public override Color MenuItemSelectedGradientBegin { get { return Theme.AccentDeep; } }
        public override Color MenuItemSelectedGradientEnd { get { return Theme.AccentDeep; } }
        public override Color MenuItemPressedGradientBegin { get { return Theme.SurfaceRaised; } }
        public override Color MenuItemPressedGradientEnd { get { return Theme.SurfaceRaised; } }
        public override Color SeparatorDark { get { return Theme.Border; } }
        public override Color SeparatorLight { get { return Theme.Border; } }
    }

    internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkColorTable())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected)
            {
                base.OnRenderMenuItemBackground(e);
                return;
            }
            Rectangle rect = new Rectangle(3, 1, e.Item.Width - 6, e.Item.Height - 2);
            Theme.FillRounded(e.Graphics, rect, 6, Theme.AccentDeep);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Selected ? Color.White : Theme.TextSoft;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (Pen pen = new Pen(Theme.Border, 1f))
            {
                Rectangle rect = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            }
        }
    }
}
