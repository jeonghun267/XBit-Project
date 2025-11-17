// XBit/Theme.cs (AccentColor 추가)

using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace XBit
{
    public enum AppTheme { Light, Dark }

    public static class Theme
    {
        public static AppTheme Current { get; private set; } = AppTheme.Light;
        public static event Action ThemeChanged;

        // ===== Palette =====
        public static Color BgMain => Current == AppTheme.Dark ? Color.FromArgb(30, 32, 36) : Color.FromArgb(245, 247, 250);
        public static Color BgSidebar => Current == AppTheme.Dark ? Color.FromArgb(36, 38, 43) : Color.FromArgb(248, 249, 251);
        public static Color BgCard => Current == AppTheme.Dark ? Color.FromArgb(42, 45, 50) : Color.White;

        public static Color FgDefault => Current == AppTheme.Dark ? Color.White : Color.Black;
        public static Color FgPrimary => Current == AppTheme.Dark ? Color.Gainsboro : Color.Black;
        public static Color FgMuted => Current == AppTheme.Dark ? Color.Silver : Color.DimGray;
        
        public static Color Hover => Current == AppTheme.Dark ? Color.FromArgb(53, 57, 63) : Color.FromArgb(236, 239, 244);
        public static Color Selected => Current == AppTheme.Dark ? Color.FromArgb(60, 65, 72) : Color.FromArgb(232, 235, 241);
        public static Color Border => Current == AppTheme.Dark ? Color.FromArgb(65, 70, 78) : Color.FromArgb(228, 231, 235);
        
        // ⭐️ Primary 색상 추가
        public static Color Primary => Color.FromArgb(66, 133, 244); // Blue
        public static Color Accent => Color.FromArgb(66, 133, 244);
        public static Color AccentColor => Color.FromArgb(66, 133, 244); // PageHome에서 사용
        public static Color Danger => Color.FromArgb(244, 67, 54); // Red

        public static void Set(AppTheme t)
        {
            if (Current == t) return;
            Current = t;
            ThemeChanged?.Invoke();
        }

        // ===== Styling helpers =====
        public static void StyleCard(Panel p)
        {
            p.BackColor = BgCard;
            p.Padding = new Padding(16);
        }

        public static void StyleTitle(Label l)
        {
            l.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            l.ForeColor = FgPrimary;
            l.Height = 28;
            l.Dock = DockStyle.Top;
        }

        public static void StyleMuted(Label l)
        {
            l.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            l.ForeColor = FgMuted;
        }

        public static void StyleNavButton(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.TextAlign = ContentAlignment.MiddleLeft;
            b.Padding = new Padding(14, 0, 10, 0);
            b.Height = 44;
            b.Font = new Font("Segoe UI", 10f);
            b.BackColor = BgSidebar;
            b.ForeColor = FgPrimary;
            b.FlatAppearance.MouseOverBackColor = Hover;
            b.Dock = DockStyle.Top;
        }

        public static void StyleDanger(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Current == AppTheme.Dark ? Color.FromArgb(120, 30, 30) : Color.FromArgb(255, 235, 235);
            b.ForeColor = Current == AppTheme.Dark ? Color.White : Color.FromArgb(180, 0, 0);
            b.Height = 36;
        }

        public static void StyleButton(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Border;
            b.BackColor = BgCard;
            b.ForeColor = FgPrimary;
            b.Font = new Font("Segoe UI", 9f);
            b.Height = 30;
            b.FlatAppearance.MouseOverBackColor = Hover;
        }

        public static void StylePrimaryButton(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Primary;
            b.ForeColor = Color.White;
            b.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            b.Height = 36;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 110, 220);
        }

        public static void EnableDoubleBuffer(Control c)
        {
            var prop = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            prop?.SetValue(c, true, null);
        }

        public static void Apply(Control root)
        {
            void Walk(Control c)
            {
                switch (c)
                {
                    case Form f:
                        f.BackColor = BgMain;
                        f.ForeColor = FgPrimary;
                        break;
                    case Panel p:
                        if (p.Dock == DockStyle.Left) p.BackColor = BgSidebar;
                        else if (p.Dock == DockStyle.Top && p.Height <= 60) p.BackColor = BgCard;
                        else if (p.Tag as string == "card") p.BackColor = BgCard;
                        else p.BackColor = BgMain;
                        break;
                    case Label l:
                        if (l.Tag as string == "muted") l.ForeColor = FgMuted; else l.ForeColor = FgDefault;
                        break;
                    case CheckBox cb:
                        cb.ForeColor = FgDefault;
                        break;
                    case DataGridView g:
                        g.BackgroundColor = BgCard;
                        g.BorderStyle = BorderStyle.None;
                        g.EnableHeadersVisualStyles = false;
                        g.ColumnHeadersDefaultCellStyle.BackColor = Hover;
                        g.ColumnHeadersDefaultCellStyle.ForeColor = FgPrimary;
                        g.DefaultCellStyle.BackColor = BgCard;
                        g.DefaultCellStyle.ForeColor = FgDefault;
                        g.DefaultCellStyle.SelectionBackColor = Accent;
                        g.DefaultCellStyle.SelectionForeColor = Color.White;
                        g.GridColor = Border;
                        break;
                    case TextBox tb:
                        tb.BackColor = BgCard;
                        tb.ForeColor = FgDefault;
                        tb.BorderStyle = BorderStyle.FixedSingle;
                        break;
                    case RichTextBox rtb:
                        rtb.BackColor = BgCard;
                        rtb.ForeColor = FgDefault;
                        rtb.BorderStyle = BorderStyle.FixedSingle;
                        break;
                    case Button b:
                        if (b.Dock != DockStyle.Top) b.ForeColor = FgDefault;
                        break;
                }
                foreach (Control child in c.Controls) Walk(child);
            }
            Walk(root);
            root.Invalidate(true);
        }
    }
}