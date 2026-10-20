using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace NaturalClicker
{
    internal static class Theme
    {
        public static readonly Color Background = Color.FromArgb(9, 13, 20);
        public static readonly Color BackgroundSoft = Color.FromArgb(12, 18, 27);
        public static readonly Color Surface = Color.FromArgb(18, 25, 36);
        public static readonly Color SurfaceRaised = Color.FromArgb(23, 32, 45);
        public static readonly Color Input = Color.FromArgb(14, 21, 31);
        public static readonly Color Border = Color.FromArgb(39, 53, 72);
        public static readonly Color BorderSoft = Color.FromArgb(30, 42, 57);
        public static readonly Color Text = Color.FromArgb(235, 242, 251);
        public static readonly Color TextSoft = Color.FromArgb(202, 214, 230);
        public static readonly Color Muted = Color.FromArgb(137, 153, 174);
        public static readonly Color MutedDark = Color.FromArgb(94, 111, 132);
        public static readonly Color Accent = Color.FromArgb(78, 140, 255);
        public static readonly Color AccentDeep = Color.FromArgb(54, 101, 228);
        public static readonly Color Green = Color.FromArgb(54, 211, 153);
        public static readonly Color Yellow = Color.FromArgb(246, 188, 81);
        public static readonly Color Red = Color.FromArgb(255, 93, 109);
        public static readonly Color RedDeep = Color.FromArgb(202, 53, 72);

        public static readonly Font Ui8 = CreateFont(8f, FontStyle.Regular, false);
        public static readonly Font Ui85 = CreateFont(8.5f, FontStyle.Regular, false);
        public static readonly Font Ui9 = CreateFont(9f, FontStyle.Regular, false);
        public static readonly Font Ui95 = CreateFont(9.5f, FontStyle.Regular, false);
        public static readonly Font Ui10 = CreateFont(10f, FontStyle.Regular, false);
        public static readonly Font Ui105 = CreateFont(10.5f, FontStyle.Regular, false);
        public static readonly Font Ui9Bold = CreateFont(9f, FontStyle.Bold, false);
        public static readonly Font Ui11Bold = CreateFont(11f, FontStyle.Bold, false);
        public static readonly Font Ui12Bold = CreateFont(12f, FontStyle.Bold, false);
        public static readonly Font Ui13Bold = CreateFont(13f, FontStyle.Bold, false);
        public static readonly Font Num34 = CreateFont(34f, FontStyle.Bold, true);
        public static readonly Font Num22 = CreateFont(22f, FontStyle.Bold, true);

        private static Font CreateFont(float size, FontStyle style, bool digits)
        {
            string family = digits ? "Bahnschrift" : "Microsoft YaHei UI";
            try
            {
                Font font = new Font(family, size, style, GraphicsUnit.Point);
                if (string.Equals(font.Name, family, StringComparison.OrdinalIgnoreCase))
                {
                    return font;
                }
                font.Dispose();
            }
            catch
            {
            }
            return new Font("Segoe UI", size, style, GraphicsUnit.Point);
        }

        public static GraphicsPath RoundedPath(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return path;
            }
            if (radius <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void FillRounded(Graphics g, Rectangle bounds, int radius, Color color)
        {
            using (GraphicsPath path = RoundedPath(bounds, radius))
            using (SolidBrush brush = new SolidBrush(color))
            {
                g.FillPath(brush, path);
            }
        }

        public static void DrawRoundedBorder(Graphics g, Rectangle bounds, int radius, Color color, float width)
        {
            using (GraphicsPath path = RoundedPath(bounds, radius))
            using (Pen pen = new Pen(color, width))
            {
                g.DrawPath(pen, path);
            }
        }

        public static Color Blend(Color a, Color b, double amount)
        {
            if (amount < 0) amount = 0;
            if (amount > 1) amount = 1;
            return Color.FromArgb(
                (int)Math.Round(a.A + (b.A - a.A) * amount),
                (int)Math.Round(a.R + (b.R - a.R) * amount),
                (int)Math.Round(a.G + (b.G - a.G) * amount),
                (int)Math.Round(a.B + (b.B - a.B) * amount));
        }

        public static Color WithAlpha(Color color, int alpha)
        {
            if (alpha < 0) alpha = 0;
            if (alpha > 255) alpha = 255;
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }

        public static void DrawText(Graphics g, string text, Font font, Color color, Rectangle bounds,
            TextFormatFlags flags)
        {
            TextRenderer.DrawText(g, text, font, bounds, color, flags);
        }

        public static TextFormatFlags LeftMiddle
        {
            get
            {
                return TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                       TextFormatFlags.SingleLine | TextFormatFlags.NoPadding |
                       TextFormatFlags.EndEllipsis;
            }
        }

        public static string FormatDuration(double seconds)
        {
            if (seconds < 0) seconds = 0;
            TimeSpan span = TimeSpan.FromSeconds(seconds);
            if (span.TotalHours >= 1)
            {
                return string.Format("{0:00}:{1:00}:{2:00}", (int)span.TotalHours, span.Minutes, span.Seconds);
            }
            return string.Format("{0:00}:{1:00}", span.Minutes, span.Seconds);
        }

        public static string FormatMs(double ms)
        {
            if (ms <= 0) return "—";
            if (ms >= 1000) return (ms / 1000.0).ToString("0.00", CultureInfo.InvariantCulture) + " s";
            return ms.ToString("0", CultureInfo.InvariantCulture) + " ms";
        }

        public static string FormatCount(long count)
        {
            return count.ToString("N0", CultureInfo.CurrentCulture);
        }
    }
}
