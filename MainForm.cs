using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using XBit.Services;
using XBit.Pages;
using XBit.Models;
using System.Reflection;
using XBit.UI; // ToastForm 사용

namespace XBit
{
    public class MainForm : Form
    {
        private Stack<NavigationEntry> NavigationStack = new Stack<NavigationEntry>();
        private NavigationEntry CurrentPage;

        public Panel pnlContent;
        private Panel pnlTopBar;
        private Button btnBack;
        private Button btnNotification;
        private Label lblNotificationBadge;
        private Timer notificationTimer;
        private NotificationService _notificationService = new NotificationService();

        // 상단 우측 상태 라벨들
        private Label lblStatusUser;
        private Label lblStatusSync;
        private Label lblStatusTime;
        private Timer clockTimer;

        public MainForm()
        {
            InitializeFormLayout();
            this.Text = $"XBit - Logged in as {AuthService.CurrentUser?.Name ?? "Guest"}";
            Theme.Apply(this);
            InternalNavigate(typeof(PageHome), null);
            UpdateBackButtonVisibility();
            InitializeNotificationTimer();
            UpdateNotificationBadge();
            InitializeStatusStrip(); // 이제 상단에 라벨로 배치
            InitializeToolTips();

            // ⭐ 이벤트 구독: 알림 생성 시 즉시 배지 갱신
            NotificationService.NotificationCreated += OnNotificationCreated;
        }

        private void InitializeFormLayout()
        {
            this.Text = "XBit";
            this.Size = new Size(1200, 800);
            this.MinimumSize = new Size(800, 600);

            pnlContent = new Panel { Name = "pnlContent", Dock = DockStyle.Fill };

            var topBar = CreateTopBar();
            // store reference to top bar for later placement of status labels
            pnlTopBar = topBar;

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlTopBar);
        }

        private Panel CreateTopBar()
        {
            var pnl = new Panel
            {
                Name = "pnlTopBar",
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Theme.BgSidebar
            };

            var btnMenu = CreateButton("☰", 50, 50, DockStyle.Left, BtnMenu_Click);
            btnBack = CreateButton("뒤로", 80, 50, DockStyle.Left, BtnBack_Click);
            btnBack.Enabled = false;

            // 알림 버튼 추가
            btnNotification = CreateButton("🔔", 50, 50, DockStyle.Right, BtnNotification_Click);
            btnNotification.Font = new Font("Segoe UI Emoji", 16f);

            // 알림 배지 (읽지 않은 알림 개수)
            lblNotificationBadge = new Label
            {
                AutoSize = false,
                Width = 20,
                Height = 20,
                BackColor = Color.Red,
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 8f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false,
                Text = "0"
            };

            pnl.Controls.Add(btnNotification);
            pnl.Controls.Add(lblNotificationBadge);

            // 컨트롤 재배치 시 배치 계산
            pnl.Resize += (s, e) =>
            {
                try
                {
                    // 알림 배지 위치
                    lblNotificationBadge.Location = new Point(pnl.Width - 35, 8);

                    if (lblStatusTime != null && lblStatusSync != null && lblStatusUser != null)
                    {
                        int gap = 12;

                        // 현재 표시될 라벨 폭(선호폭 사용)
                        int wUser = lblStatusUser.PreferredWidth;
                        int wSync = lblStatusSync.PreferredWidth;
                        int wTime = lblStatusTime.PreferredWidth;
                        int totalStatusWidth = wUser + gap + wSync + gap + wTime;

                        // 알림 배지가 보이면 그 왼쪽, 아니면 우측 끝에서 적당한 여백을 둠
                        int rightEdge = lblNotificationBadge.Visible ? lblNotificationBadge.Left - 12 : pnl.ClientSize.Width - 24;

                        // 시작 X 계산: rightEdge에서 totalStatusWidth를 뺌
                        int startX = rightEdge - totalStatusWidth;
                        // 최소 시작 X: 메뉴/뒤로 버튼과 겹치지 않도록 left padding 보장
                        int minStartX = btnMenu.Right + 12;

                        // 추가: 전체 블록을 더 왼쪽으로 약간 이동 (겹침 해소)
                        const int extraLeftShift = 28; // 필요하면 값 조정(작게: 12 / 크게: 40)
                        startX -= extraLeftShift;

                        if (startX < minStartX) startX = minStartX;

                        // 배치 (왼->오)
                        int x = startX;
                        lblStatusUser.Location = new Point(x, 16);
                        x += wUser + gap;
                        lblStatusSync.Location = new Point(x, 16);
                        x += wSync + gap;
                        lblStatusTime.Location = new Point(x, 16);

                        // z-order 보장
                        lblStatusUser.BringToFront();
                        lblStatusSync.BringToFront();
                        lblStatusTime.BringToFront();

                        // 알림과 근접 시 추가 조정(겹침 방지)
                        if (lblNotificationBadge.Visible && lblStatusTime.Right + 8 > lblNotificationBadge.Left)
                        {
                            int overlap = (lblStatusTime.Right + 8) - lblNotificationBadge.Left;
                            lblStatusUser.Left = Math.Max(minStartX, lblStatusUser.Left - overlap);
                            lblStatusSync.Left = lblStatusUser.Right + gap;
                            lblStatusTime.Left = lblStatusSync.Right + gap;
                        }
                    }
                }
                catch { /* ignore */ }
            };

            pnl.Controls.Add(btnMenu);
            pnl.Controls.Add(btnBack);

            return pnl;
        }

        private Button CreateButton(string text, int width, int height, DockStyle dock, EventHandler clickHandler)
        {
            var button = new Button
            {
                Text = text,
                Width = width,
                Height = height,
                Dock = dock,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.BgSidebar,
                ForeColor = Theme.FgPrimary
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += clickHandler;
            return button;
        }

        private void InitializeToolTips()
        {
            Theme.CreateToolTip(btnNotification, "알림");
            Theme.CreateToolTip(btnBack, "뒤로가기");
        }

        private void InitializeNotificationTimer()
        {
            // 30초마다 알림 개수 업데이트
            notificationTimer = new Timer
            {
                Interval = 30000 // 30초
            };
            notificationTimer.Tick += (s, e) => UpdateNotificationBadge();
            notificationTimer.Start();
        }

        private void InitializeStatusStrip()
        {
            lblStatusUser = new Label
            {
                AutoSize = true,
                Text = AuthService.CurrentUser != null ? $"사용자: {AuthService.CurrentUser.Name}" : "사용자: -",
                ForeColor = Theme.FgPrimary,
                BackColor = Theme.BgSidebar,
                Font = new Font("맑은 고딕", 9f, FontStyle.Regular),
                Visible = true
            };

            lblStatusSync = new Label
            {
                AutoSize = true,
                Text = "마지막 동기화: N/A",
                ForeColor = Theme.FgPrimary,
                BackColor = Theme.BgSidebar,
                Font = new Font("맑은 고딕", 9f, FontStyle.Regular),
                Visible = true
            };

            lblStatusTime = new Label
            {
                AutoSize = true,
                Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm"), // 초 제거
                ForeColor = Theme.FgPrimary,
                BackColor = Theme.BgSidebar,
                Font = new Font("맑은 고딕", 9f, FontStyle.Regular),
                Visible = true
            };

            if (pnlTopBar != null)
            {
                // 먼저 추가하고 BringToFront로 최상단 고정
                pnlTopBar.Controls.Add(lblStatusUser);
                pnlTopBar.Controls.Add(lblStatusSync);
                pnlTopBar.Controls.Add(lblStatusTime);

                lblStatusUser.BringToFront();
                lblStatusSync.BringToFront();
                lblStatusTime.BringToFront();

                // 한 번 강제 레이아웃
                pnlTopBar.PerformLayout();
                pnlTopBar.Invalidate();
            }

            // 시계 타이머 (분단위 업데이트)
            clockTimer = new Timer { Interval = 1000 };
            clockTimer.Tick += (s, e) =>
            {
                if (lblStatusTime != null)
                {
                    lblStatusTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm"); // 초 제거
                    if (pnlTopBar != null) pnlTopBar.PerformLayout();
                }
            };
            clockTimer.Start();
        }

        // 접근제한자를 public으로 변경했습니다.
        public void UpdateNotificationBadge()
        {
            if (AuthService.CurrentUser == null) return;

            int unreadCount = _notification_service_safe(AuthService.CurrentUser.Id);

            if (unreadCount > 0)
            {
                lblNotificationBadge.Text = unreadCount > 99 ? "99+" : unreadCount.ToString();
                lblNotificationBadge.Visible = true;
            }
            else
            {
                lblNotificationBadge.Visible = false;
            }

            // 레이아웃 / 화면 강제 갱신
            try
            {
                lblNotificationBadge.BringToFront();
                if (pnlTopBar != null) pnlTopBar.PerformLayout();
                lblNotificationBadge.Invalidate();
                pnlTopBar?.Invalidate();
            }
            catch { }
        }

        private int _notification_service_safe(int userId)
        {
            try
            {
                return _notificationService.GetUnreadCount(userId);
            }
            catch
            {
                return 0;
            }
        }

        private void BtnNotification_Click(object sender, EventArgs e)
        {
            NavigateTo<PageNotifications>();
            UpdateNotificationBadge(); // 알림 페이지로 이동 후 배지 업데이트
        }

        private void BtnMenu_Click(object sender, EventArgs e)
        {
            var menu = new ContextMenuStrip();

            // 기본 메뉴들
            menu.Items.Add("홈", null, (_, __) => NavigateTo<PageHome>());
            menu.Items.Add("프로젝트", null, (_, __) => NavigateTo<PageAssignments>());

            menu.Items.Add("게시판", null, (_, __) => NavigateTo<PageBoard>());
            menu.Items.Add("협업", null, (_, __) => NavigateTo<PageProjectBoard>());
            // "알림" 항목 제거됨
            menu.Items.Add("설정", null, (_, __) => NavigateTo<PageSettings>());
            menu.Items.Add("로그아웃", null, (_, __) => BtnLogout_Click(sender, e));

            menu.Show(Cursor.Position);
        }

        private void BtnBack_Click(object sender, EventArgs e) => GoBack();

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            if (notificationTimer != null)
            {
                notificationTimer.Stop();
                notificationTimer.Dispose();
            }
            clockTimer?.Stop();
            AuthService.Logout();
            Application.Restart();
        }

        public void GoBack()
        {
            if (NavigationStack.Count > 0)
            {
                NavigationEntry previousEntry = NavigationStack.Pop();
                InternalNavigate(previousEntry.PageType, previousEntry.Parameter, isBack: true);
                UpdateBackButtonVisibility();
            }
        }

        private void UpdateBackButtonVisibility()
        {
            bool isEnabled = NavigationStack.Count > 0;

            btnBack.Enabled = isEnabled;
            btnBack.Visible = true;

            btnBack.ForeColor = isEnabled ? Theme.FgPrimary : Theme.FgMuted;
        }

        public void NavigateTo<T>(object parameter = null) where T : UserControl, new()
        {
            if (CurrentPage != null && (CurrentPage.PageType != typeof(T) || CurrentPage.Parameter != parameter))
            {
                NavigationStack.Push(CurrentPage);
            }
            InternalNavigate(typeof(T), parameter);
        }

        private void InternalNavigate(Type pageType, object parameter, bool isBack = false)
        {
            foreach (Control control in pnlContent.Controls)
            {
                control.Dispose();
            }
            pnlContent.Controls.Clear();

            UserControl newPage = CreatePageInstance(pageType, parameter);

            newPage.Padding = new Padding(0);
            newPage.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(newPage);

            CurrentPage = new NavigationEntry(pageType, parameter);

            UpdateBackButtonVisibility();
        }

        private UserControl CreatePageInstance(Type pageType, object parameter)
        {
            if (pageType == typeof(PageAssignmentDetail) && parameter is int assignmentId && assignmentId != -1)
            {
                return (UserControl)Activator.CreateInstance(pageType, assignmentId);
            }
            else if (pageType == typeof(PagePostDetail) && parameter is int postId && postId != -1)
            {
                return (UserControl)Activator.CreateInstance(pageType, postId);
            }
            else if (pageType == typeof(PageAssignments) && parameter is string filter)
            {
                var page = (PageAssignments)Activator.CreateInstance(pageType);
                ((PageAssignments)page).FilterData(filter);
                return page;
            }
            else if (pageType == typeof(PageSettings) && parameter is string sectionTag)
            {
                return (UserControl)Activator.CreateInstance(pageType);
            }
            else
            {
                return (UserControl)Activator.CreateInstance(pageType);
            }
        }

        // 생성자 안 적절 위치(InitializeNotificationTimer(); UpdateNotificationBadge(); 호출 이후)에 이벤트 구독 추가
        private void OnNotificationCreated(XBit.Models.Notification notification)
        {
            try
            {
                if (this.IsHandleCreated && this.InvokeRequired)
                {
                    this.BeginInvoke((Action)(() => OnNotificationCreated(notification)));
                    return;
                }

                // 현재 로그인 사용자 대상 알림이면 배지 갱신
                if (AuthService.CurrentUser != null && notification != null && notification.UserId == AuthService.CurrentUser.Id)
                {
                    UpdateNotificationBadge();
                }
                else
                {
                    // 일반적으로도 배지 갱신
                    UpdateNotificationBadge();
                }
            }
            catch
            {
                // 무시
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                NotificationService.NotificationCreated -= OnNotificationCreated;
            }
            catch { }

            if (notificationTimer != null)
            {
                notificationTimer.Stop();
                notificationTimer.Dispose();
            }
            clockTimer?.Stop();
            base.OnFormClosing(e);
        }

        // 동기 상태를 업데이트하는 헬퍼 (MainForm 클래스에 추가)
        public void UpdateSyncStatus(string message = null)
        {
            if (lblStatusSync == null) return;

            if (string.IsNullOrEmpty(message))
            {
                lblStatusSync.Text = $"마지막 동기화: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            }
            else
            {
                lblStatusSync.Text = $"마지막 동기화: {message}";
            }

            // 위치 업데이트
            if (pnlTopBar != null) pnlTopBar.PerformLayout();
        }
    }
}