using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using XBit.Services;

namespace XBit.UI
{
    public enum ToastType { Info, Success, Warning, Error }

    public class ToastForm : Form
    {
        private readonly Label lblMessage;
        private readonly Label lblIcon;
        private readonly Panel iconPanel;
        private readonly Timer lifeTimer;
        private readonly Timer fadeTimer;
        private readonly Timer slideTimer;
        private int lifeMs;
        private bool fadingOut;
        private int targetY;
        private int slideStartY;

        // Active toasts management
        private static readonly List<ToastForm> _activeToasts = new List<ToastForm>();
        private const int MarginRight = 20;
        private const int MarginBottom = 20;
        private const int StackOffset = 15;

        // Screen identifier for placement
        private string _screenDevice;

        public ToastForm(string message, ToastType type = ToastType.Info, int durationMs = 3000)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            Width = 380;
            Height = 80;
            Opacity = 0;
            lifeMs = durationMs;

            BackColor = Color.White;
            
            // 그림자 효과를 위한 region 설정
            var path = CreateRoundedRectangle(0, 0, Width, Height, 12);
            Region = new Region(path);

            var container = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0)
            };

            // 왼쪽 색상 바
            var colorBar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 6,
                BackColor = GetAccentColor(type)
            };

            // 아이콘 패널
            iconPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 60,
                BackColor = Color.Transparent
            };

            lblIcon = new Label
            {
                Text = GetIcon(type),
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = GetAccentColor(type),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            iconPanel.Controls.Add(lblIcon);

            // 메시지 레이블
            lblMessage = new Label
            {
                Text = message,
                ForeColor = Color.FromArgb(50, 50, 50),
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 0, 20, 0)
            };

            // 닫기 버튼 (ASCII 문자로 대체)
            var closeBtn = new Label
            {
                Text = "x",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.Gray,
                AutoSize = false,
                Size = new Size(40, 80),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand
            };
            closeBtn.MouseEnter += (s, e) => closeBtn.ForeColor = Color.FromArgb(200, 0, 0);
            closeBtn.MouseLeave += (s, e) => closeBtn.ForeColor = Color.Gray;
            closeBtn.Click += (s, e) => CloseToast();

            container.Controls.Add(colorBar);
            container.Controls.Add(iconPanel);
            container.Controls.Add(lblMessage);
            container.Controls.Add(closeBtn);
            Controls.Add(container);

            // 그림자 효과
            container.Paint += (s, e) =>
            {
                using (var shadowPath = CreateRoundedRectangle(0, 0, Width, Height, 12))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var shadow = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                    {
                        e.Graphics.FillPath(shadow, shadowPath);
                    }
                }
            };

            // lifespan timer
            lifeTimer = new Timer { Interval = lifeMs };
            lifeTimer.Tick += (s, e) =>
            {
                lifeTimer.Stop();
                StartFadeOut();
            };

            // fade animation timer
            fadeTimer = new Timer { Interval = 30 };
            fadeTimer.Tick += FadeTick;

            // slide animation timer
            slideTimer = new Timer { Interval = 15 };
            slideTimer.Tick += SlideTick;

            // mouse interactions: stop/start lifespan + 호버 효과
            this.MouseEnter += OnMouseEnterToast;
            this.MouseLeave += OnMouseLeaveToast;
            container.MouseEnter += OnMouseEnterToast;
            container.MouseLeave += OnMouseLeaveToast;
            lblMessage.MouseEnter += OnMouseEnterToast;
            lblMessage.MouseLeave += OnMouseLeaveToast;
            iconPanel.MouseEnter += OnMouseEnterToast;
            iconPanel.MouseLeave += OnMouseLeaveToast;

            // click to close immediately
            this.Click += (s, e) => CloseToast();
            container.Click += (s, e) => CloseToast();
            lblMessage.Click += (s, e) => CloseToast();
            iconPanel.Click += (s, e) => CloseToast();
            lblIcon.Click += (s, e) => CloseToast();
        }

        private void OnMouseEnterToast(object sender, EventArgs e)
        {
            lifeTimer.Stop();
            // 호버 시 약간 확대 효과
            this.Cursor = Cursors.Hand;
        }

        private void OnMouseLeaveToast(object sender, EventArgs e)
        {
            if (!fadingOut) lifeTimer.Start();
            this.Cursor = Cursors.Default;
        }

        private GraphicsPath CreateRoundedRectangle(int x, int y, int width, int height, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
            path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
            path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }

        // 특수 유니코드 기호 대신 ASCII 대체 문자열 사용
        private string GetIcon(ToastType type)
        {
            switch (type)
            {
                case ToastType.Success: return "OK";
                case ToastType.Warning: return "!";
                case ToastType.Error: return "X";
                case ToastType.Info:
                default: return "i";
            }
        }

        private Color GetAccentColor(ToastType type)
        {
            switch (type)
            {
                case ToastType.Success: return Color.FromArgb(76, 175, 80);
                case ToastType.Warning: return Color.FromArgb(255, 152, 0);
                case ToastType.Error: return Color.FromArgb(244, 67, 54);
                case ToastType.Info:
                default: return Color.FromArgb(33, 150, 243);
            }
        }

        // Prevent stealing focus and hide from alt-tab
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                const int WS_EX_NOACTIVATE = 0x08000000;
                const int WS_EX_TOOLWINDOW = 0x00000080;
                const int CS_DROPSHADOW = 0x00020000;
                cp.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (!_activeToasts.Contains(this))
                _activeToasts.Add(this);

            RepositionToasts();
            StartSlideIn();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (_activeToasts.Contains(this))
            {
                _activeToasts.Remove(this);
                RepositionToasts();
            }
        }

        private void RepositionToasts()
        {
            try
            {
                // group by screen device and stack per screen
                var groups = _activeToasts.GroupBy(t => t._screenDevice ?? Screen.PrimaryScreen.DeviceName);

                foreach (var group in groups)
                {
                    var screen = Screen.AllScreens.FirstOrDefault(s => s.DeviceName == group.Key) ?? Screen.PrimaryScreen;
                    var working = screen.WorkingArea;
                    int x = working.Right - Width - MarginRight;
                    int bottom = working.Bottom - MarginBottom;

                    var groupToasts = group.ToList();
                    for (int i = 0; i < groupToasts.Count; i++)
                    {
                        var t = groupToasts[i];
                        int y = bottom - (i + 1) * (t.Height + StackOffset) + StackOffset;
                        
                        if (t.slideTimer.Enabled)
                        {
                            t.targetY = y;
                        }
                        else
                        {
                            if (t.InvokeRequired)
                            {
                                t.BeginInvoke(new Action(() => t.Location = new Point(x, y)));
                            }
                            else
                            {
                                t.Location = new Point(x, y);
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore positioning errors
            }
        }

        private void StartSlideIn()
        {
            fadingOut = false;
            Opacity = 0;
            
            var screen = Screen.AllScreens.FirstOrDefault(s => s.DeviceName == _screenDevice) ?? Screen.PrimaryScreen;
            var working = screen.WorkingArea;
            int x = working.Right - Width - MarginRight;
            slideStartY = working.Bottom;
            targetY = Location.Y;
            Location = new Point(x, slideStartY);
            
            fadeTimer.Start();
            slideTimer.Start();
        }

        private void StartFadeOut()
        {
            fadingOut = true;
            slideTimer.Stop();
            fadeTimer.Start();
        }

        private void SlideTick(object sender, EventArgs e)
        {
            try
            {
                if (Location.Y > targetY)
                {
                    int newY = Math.Max(targetY, Location.Y - 15);
                    Location = new Point(Location.X, newY);
                }
                else
                {
                    slideTimer.Stop();
                }
            }
            catch
            {
                slideTimer.Stop();
            }
        }

        private void FadeTick(object sender, EventArgs e)
        {
            try
            {
                if (!fadingOut)
                {
                    Opacity = Math.Min(1.0, Opacity + 0.15);
                    if (Opacity >= 1.0)
                    {
                        fadeTimer.Stop();
                        lifeTimer.Start();
                    }
                }
                else
                {
                    Opacity = Math.Max(0.0, Opacity - 0.15);
                    if (Opacity <= 0.0)
                    {
                        fadeTimer.Stop();
                        Close();
                    }
                }
            }
            catch
            {
                fadeTimer.Stop();
                Close();
            }
        }

        private void CloseToast()
        {
            lifeTimer.Stop();
            slideTimer.Stop();
            StartFadeOut();
        }

        // Thread-safe static helper
        public static void ShowToast(Form owner, string message, ToastType type = ToastType.Info, int durationMs = 3000)
        {
            if (owner == null || owner.IsDisposed) return;

            if (owner.InvokeRequired)
            {
                owner.BeginInvoke(new Action(() => ShowToastInternal(owner, message, type, durationMs)));
            }
            else
            {
                ShowToastInternal(owner, message, type, durationMs);
            }
        }

        private static void ShowToastInternal(Form owner, string message, ToastType type, int durationMs)
        {
            var toast = new ToastForm(message, type, durationMs);

            // If there are active toasts, reuse their screen; otherwise use main form screen or primary screen
            if (_activeToasts.Count > 0)
            {
                toast._screenDevice = _activeToasts.Last()._screenDevice ?? Screen.PrimaryScreen.DeviceName;
            }
            else
            {
                try
                {
                    var mainForm = Application.OpenForms.OfType<Form>().FirstOrDefault(f => f.GetType().Name == "MainForm");
                    if (mainForm != null && !mainForm.IsDisposed && mainForm.Visible)
                    {
                        toast._screenDevice = Screen.FromControl(mainForm).DeviceName;
                    }
                    else
                    {
                        toast._screenDevice = Screen.PrimaryScreen.DeviceName;
                    }
                }
                catch
                {
                    toast._screenDevice = Screen.PrimaryScreen.DeviceName;
                }
            }

            toast.Show();
            toast.BringToFront();
        }
    }
}