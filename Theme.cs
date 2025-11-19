// XBit/Theme.cs (유니코드 깨짐 수정본)

using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Linq;

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
        
        // 색상 팔레트
        public static Color Primary => Color.FromArgb(66, 133, 244); // Blue
        public static Color Accent => Color.FromArgb(66, 133, 244);
        public static Color AccentColor => Color.FromArgb(66, 133, 244);
        public static Color Danger => Color.FromArgb(244, 67, 54); // Red
        public static Color Success => Color.FromArgb(76, 175, 80); // Green
        public static Color Warning => Color.FromArgb(255, 152, 0); // Orange
        public static Color Info => Color.FromArgb(3, 169, 244); // Light Blue

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

        // ===== UI 헬퍼: ToolTip, Loading Overlay, 통계 helper들 =====

        public static ToolTip CreateToolTip(Control control, string text)
        {
            if (control == null) return null;
            var tt = new ToolTip
            {
                AutoPopDelay = 5000,
                InitialDelay = 400,
                ReshowDelay = 100,
                ShowAlways = true
            };
            tt.SetToolTip(control, text);
            return tt;
        }

        public static Panel CreateLoadingOverlay(string message = "로딩 중...")
        {
            var overlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(120, 0, 0, 0),
                Visible = false
            };

            var box = new Panel
            {
                Width = 320,
                Height = 90,
                BackColor = BgCard,
                Padding = new Padding(12),
                BorderStyle = BorderStyle.None
            };
            box.Anchor = AnchorStyles.None;

            var lbl = new Label
            {
                Text = message,
                Font = new Font("맑은 고딕", 11f, FontStyle.Bold),
                ForeColor = FgDefault,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            var prg = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Height = 16,
                Dock = DockStyle.Bottom
            };

            box.Controls.Add(lbl);
            box.Controls.Add(prg);
            overlay.Controls.Add(box);

            // 위치 보정: 부모에 추가된 후 위치 재설정 필요
            overlay.Resize += (s, e) =>
            {
                box.Location = new Point(Math.Max(10, (overlay.Width - box.Width) / 2), Math.Max(10, (overlay.Height - box.Height) / 2));
            };

            return overlay;
        }

        // 통계 UI 헬퍼들
        public static Panel CreateStatCard(string title, string value, Color accentColor, int width = 200, int height = 120)
        {
            var card = new Panel
            {
                Width = width,
                Height = height,
                BackColor = BgCard,
                Margin = new Padding(10)
            };
            StyleCard(card);

            var accentBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 4,
                BackColor = accentColor
            };

            var lblValue = new Label
            {
                Text = value,
                Font = new Font("맑은 고딕", 24f, FontStyle.Bold),
                ForeColor = accentColor,
                AutoSize = true,
                Location = new Point(20, 25)
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("맑은 고딕", 10f),
                ForeColor = FgMuted,
                AutoSize = true,
                Location = new Point(20, 70)
            };

            card.Controls.Add(accentBar);
            card.Controls.Add(lblValue);
            card.Controls.Add(lblTitle);

            return card;
        }

        public static Panel CreateProgressBar(int current, int total, Color barColor, int width = 200, int height = 20)
        {
            var container = new Panel
            {
                Width = width,
                Height = height,
                BackColor = BgMain,
                BorderStyle = BorderStyle.FixedSingle
            };

            int percentage = total > 0 ? (int)((double)current / total * 100) : 0;
            int barWidth = total > 0 ? (int)((double)current / total * (width - 2)) : 0;

            var bar = new Panel
            {
                Width = barWidth,
                Height = height - 2,
                BackColor = barColor,
                Location = new Point(1, 1)
            };

            var lblPercentage = new Label
            {
                Text = $"{percentage}%",
                Font = new Font("맑은 고딕", 8f, FontStyle.Bold),
                ForeColor = percentage > 50 ? Color.White : FgDefault,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Width = width,
                Height = height,
                Location = new Point(0, 0),
                BackColor = Color.Transparent
            };

            container.Controls.Add(bar);
            container.Controls.Add(lblPercentage);
            lblPercentage.BringToFront();

            return container;
        }

        public static Panel CreateDonutChart(int value, int total, Color fillColor, string label, int size = 100)
        {
            var container = new Panel
            {
                Width = size + 40,
                Height = size + 60,
                BackColor = Color.Transparent
            };

            var chartPanel = new Panel
            {
                Width = size,
                Height = size,
                Location = new Point(20, 10),
                BackColor = Color.Transparent
            };

            chartPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // 배경 원
                using (var bgBrush = new SolidBrush(BgMain))
                {
                    e.Graphics.FillEllipse(bgBrush, 0, 0, size, size);
                }

                // 진행률 호
                if (total > 0)
                {
                    float percentage = (float)value / total;
                    float sweepAngle = 360 * percentage;

                    using (var fillBrush = new SolidBrush(fillColor))
                    {
                        e.Graphics.FillPie(fillBrush, 0, 0, size, size, -90, sweepAngle);
                    }
                }

                // 중앙 흰색 원 (도넛 효과)
                int innerSize = (int)(size * 0.6);
                int innerOffset = (size - innerSize) / 2;
                using (var innerBrush = new SolidBrush(BgCard))
                {
                    e.Graphics.FillEllipse(innerBrush, innerOffset, innerOffset, innerSize, innerSize);
                }

                // 백분율 텍스트
                int percentage_int = total > 0 ? (int)((float)value / total * 100) : 0;
                string text = $"{percentage_int}%";
                using (var font = new Font("맑은 고딕", 16f, FontStyle.Bold))
                using (var textBrush = new SolidBrush(FgDefault))
                {
                    var textSize = e.Graphics.MeasureString(text, font);
                    e.Graphics.DrawString(text, font, textBrush,
                        (size - textSize.Width) / 2,
                        (size - textSize.Height) / 2);
                }
            };

            var lblLabel = new Label
            {
                Text = label,
                Font = new Font("맑은 고딕", 9f),
                ForeColor = FgMuted,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Width = size + 40,
                Height = 30,
                Location = new Point(0, size + 20)
            };

            container.Controls.Add(chartPanel);
            container.Controls.Add(lblLabel);

            return container;
        }

        public static Panel CreateTrendLine(int[] values, Color lineColor, int width = 200, int height = 60)
        {
            var panel = new Panel
            {
                Width = width,
                Height = height,
                BackColor = Color.Transparent
            };

            panel.Paint += (s, e) =>
            {
                if (values == null || values.Length < 2) return;

                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                int max = values.Max();
                if (max == 0) max = 1;

                float xStep = (float)width / (values.Length - 1);

                using (var pen = new Pen(lineColor, 2))
                {
                    for (int i = 0; i < values.Length - 1; i++)
                    {
                        float x1 = i * xStep;
                        float y1 = height - (float)values[i] / max * height;
                        float x2 = (i + 1) * xStep;
                        float y2 = height - (float)values[i + 1] / max * height;

                        e.Graphics.DrawLine(pen, x1, y1, x2, y2);
                    }
                }
            };

            return panel;
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