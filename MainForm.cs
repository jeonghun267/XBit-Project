using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using XBit.Services;
using XBit.Pages;
using XBit.Models;
using System.Reflection;

namespace XBit
{
    public class MainForm : Form
    {
        private Stack<NavigationEntry> NavigationStack = new Stack<NavigationEntry>();
        private NavigationEntry CurrentPage;

        public Panel pnlContent;
        private Button btnBack;
        private Button btnNotification;
        private Label lblNotificationBadge;
        private Timer notificationTimer;
        private NotificationService _notificationService = new NotificationService();

        public MainForm()
        {
            InitializeFormLayout();
            this.Text = $"XBit - Logged in as {AuthService.CurrentUser?.Name ?? "Guest"}";
            Theme.Apply(this);
            InternalNavigate(typeof(PageHome), null);
            UpdateBackButtonVisibility();
            InitializeNotificationTimer();
            UpdateNotificationBadge();
        }

        private void InitializeFormLayout()
        {
            this.Text = "XBit";
            this.Size = new Size(1200, 800);
            this.MinimumSize = new Size(800, 600);

            pnlContent = new Panel { Name = "pnlContent", Dock = DockStyle.Fill };

            var pnlTopBar = CreateTopBar();
            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlTopBar);
        }

        private Panel CreateTopBar()
        {
            var pnlTopBar = new Panel
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

            // 배지를 알림 버튼 위에 배치
            pnlTopBar.Controls.Add(btnNotification);
            pnlTopBar.Controls.Add(lblNotificationBadge);
            
            pnlTopBar.Resize += (s, e) =>
            {
                lblNotificationBadge.Location = new Point(
                    pnlTopBar.Width - 35,
                    8
                );
            };

            pnlTopBar.Controls.Add(btnMenu);
            pnlTopBar.Controls.Add(btnBack);

            return pnlTopBar;
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

        private void UpdateNotificationBadge()
        {
            if (AuthService.CurrentUser == null) return;

            int unreadCount = _notificationService.GetUnreadCount(AuthService.CurrentUser.Id);

            if (unreadCount > 0)
            {
                lblNotificationBadge.Text = unreadCount > 99 ? "99+" : unreadCount.ToString();
                lblNotificationBadge.Visible = true;
            }
            else
            {
                lblNotificationBadge.Visible = false;
            }
        }

        private void BtnNotification_Click(object sender, EventArgs e)
        {
            NavigateTo<PageNotifications>();
            UpdateNotificationBadge(); // 알림 페이지로 이동 후 배지 업데이트
        }

        private void BtnMenu_Click(object sender, EventArgs e)
        {
            var menuItems = new Dictionary<string, Action>
            {
                { "홈", () => NavigateTo<PageHome>() },
                { "프로젝트", () => NavigateTo<PageAssignments>() },
                { "게시판", () => NavigateTo<PageBoard>() },
                { "협업", () => NavigateTo<PageProjectBoard>() },
                { "알림", () => NavigateTo<PageNotifications>() },
                { "설정", () => NavigateTo<PageSettings>() },
                { "로그아웃", () => BtnLogout_Click(sender, e) }
            };

            var menu = new ContextMenuStrip();
            foreach (var item in menuItems)
            {
                menu.Items.Add(item.Key, null, (_, __) => item.Value());
            }

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
                page.FilterData(filter);
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (notificationTimer != null)
            {
                notificationTimer.Stop();
                notificationTimer.Dispose();
            }
            base.OnFormClosing(e);
        }
    }
}