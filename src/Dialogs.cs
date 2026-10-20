using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NaturalClicker
{
    internal sealed class CountdownOverlay : Form
    {
        private string _title = "准备开始";
        private string _subtitle = "";
        private int _remaining = 3;
        private int _total = 3;

        public CountdownOverlay()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(346, 96);
            BackColor = Theme.Background;
            Opacity = 0.97;
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                cp.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT (click-through)
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }

        public void ShowCountdown(string title, string subtitle, int totalSeconds)
        {
            _title = title;
            _subtitle = subtitle;
            _total = Math.Max(1, totalSeconds);
            _remaining = _total;
            PositionNearCursorScreen();
            if (!Visible) Show();
            BringToFront();
            Invalidate();
        }

        public void SetCountdown(int remaining, int total)
        {
            _remaining = Math.Max(0, remaining);
            _total = Math.Max(1, total);
            Invalidate();
        }

        public void HideOverlay()
        {
            if (Visible) Hide();
        }

        private void PositionNearCursorScreen()
        {
            Screen screen = Screen.FromPoint(Cursor.Position);
            Rectangle bounds = screen.Bounds;
            int x = bounds.Left + (bounds.Width - Width) / 2;
            int y = bounds.Top + 70;
            Location = new Point(x, y);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width > 0 && Height > 0)
            {
                using (GraphicsPath path = Theme.RoundedPath(new Rectangle(0, 0, Width, Height), 20))
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
            using (GraphicsPath path = Theme.RoundedPath(rect, 20))
            using (LinearGradientBrush brush = new LinearGradientBrush(
                rect,
                Color.FromArgb(30, 42, 59),
                Color.FromArgb(17, 24, 35),
                25f))
            {
                g.FillPath(brush, path);
                using (Pen pen = new Pen(Color.FromArgb(72, 94, 122), 1f))
                {
                    g.DrawPath(pen, path);
                }
            }

            Rectangle circle = new Rectangle(21, 20, 56, 56);
            using (SolidBrush circleFill = new SolidBrush(Color.FromArgb(29, 39, 54)))
            {
                g.FillEllipse(circleFill, circle);
            }
            using (Pen track = new Pen(Color.FromArgb(52, 68, 89), 3f))
            {
                g.DrawEllipse(track, circle);
            }

            float ratio = _total > 0 ? (float)_remaining / _total : 0f;
            if (_remaining > 0)
            {
                using (Pen arc = new Pen(Theme.Accent, 3f))
                {
                    arc.StartCap = LineCap.Round;
                    arc.EndCap = LineCap.Round;
                    g.DrawArc(arc, circle, -90f, 360f * ratio);
                }
            }
            else
            {
                using (Pen arc = new Pen(Theme.Green, 3f))
                {
                    arc.StartCap = LineCap.Round;
                    arc.EndCap = LineCap.Round;
                    g.DrawArc(arc, circle, -90f, 359f);
                }
            }

            string digit = _remaining > 0 ? _remaining.ToString() : "•";
            TextRenderer.DrawText(g, digit, Theme.Num22, circle, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

            TextRenderer.DrawText(g, _title, Theme.Ui11Bold,
                new Rectangle(94, 23, Width - 114, 24), Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding |
                TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, _subtitle, Theme.Ui85,
                new Rectangle(95, 48, Width - 116, 28), Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding |
                TextFormatFlags.EndEllipsis);
        }
    }

    internal sealed class PromptDialog : Form
    {
        private readonly InputField _nameField;
        private readonly Label _errorLabel;

        public string PresetName { get; private set; }

        public PromptDialog()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(420, 210);
            BackColor = Theme.Background;
            KeyPreview = true;
            Text = "保存方案";

            CaptionButton close = new CaptionButton();
            close.Glyph = "x";
            close.Danger = true;
            close.Size = new Size(34, 28);
            close.Location = new Point(374, 12);
            close.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(close);

            Label title = new Label();
            title.Text = "保存当前参数为方案";
            title.Font = Theme.Ui12Bold;
            title.ForeColor = Theme.Text;
            title.BackColor = Color.Transparent;
            title.Location = new Point(24, 18);
            title.Size = new Size(330, 26);
            Controls.Add(title);

            Label desc = new Label();
            desc.Text = "方案会保存在本机，下次可从「参数方案」中直接载入。同名方案将被覆盖。";
            desc.Font = Theme.Ui85;
            desc.ForeColor = Theme.Muted;
            desc.BackColor = Color.Transparent;
            desc.Location = new Point(24, 47);
            desc.Size = new Size(370, 22);
            Controls.Add(desc);

            _nameField = new InputField();
            _nameField.Location = new Point(24, 79);
            _nameField.Size = new Size(372, 38);
            _nameField.Suffix = "";
            _nameField.Value = "";
            Controls.Add(_nameField);

            _errorLabel = new Label();
            _errorLabel.Text = "";
            _errorLabel.Font = Theme.Ui85;
            _errorLabel.ForeColor = Theme.Red;
            _errorLabel.BackColor = Color.Transparent;
            _errorLabel.Location = new Point(24, 121);
            _errorLabel.Size = new Size(372, 20);
            Controls.Add(_errorLabel);

            AppButton cancel = new AppButton();
            cancel.Text = "取消";
            cancel.Size = new Size(86, 38);
            cancel.Location = new Point(190, 152);
            cancel.FillColor = Theme.SurfaceRaised;
            cancel.HoverColor = Color.FromArgb(32, 45, 61);
            cancel.TextColor = Theme.TextSoft;
            cancel.BorderColor = Theme.Border;
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);

            AppButton save = new AppButton();
            save.Text = "保存方案";
            save.Size = new Size(112, 38);
            save.Location = new Point(284, 152);
            save.FillColor = Theme.AccentDeep;
            save.HoverColor = Theme.Accent;
            save.TextColor = Color.White;
            save.BorderColor = Color.FromArgb(0, 0, 0, 0);
            save.Click += delegate { Confirm(); };
            Controls.Add(save);

            Shown += delegate
            {
                _nameField.FocusInput();
                Invalidate();
            };
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width > 0 && Height > 0)
            {
                using (GraphicsPath path = Theme.RoundedPath(new Rectangle(0, 0, Width, Height), 18))
                {
                    Region old = Region;
                    Region = new Region(path);
                    if (old != null) old.Dispose();
                }
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = Theme.RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), 18))
            using (Pen pen = new Pen(Theme.Border, 1f))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && e.Y < 50)
            {
                Native.DragWindow(Handle);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                Confirm();
            }
        }

        private void Confirm()
        {
            string name = (_nameField.Value ?? string.Empty).Trim();
            if (name.Length == 0)
            {
                _errorLabel.Text = "请输入方案名称";
                _nameField.FocusInput();
                return;
            }
            PresetName = name;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
