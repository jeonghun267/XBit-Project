using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using XBit.Services;
using XBit.Pages;
using System.Reflection;

namespace XBit
{
    public class MainForm : Form
    {
        private Stack<NavigationEntry> NavigationStack = new Stack<NavigationEntry>();
        private NavigationEntry CurrentPage;

        public Panel pnlContent;
        private FlowLayoutPanel pnlSidebar;
        private Button btnBack;

        private Button btnNotifications;
        private NotificationService _notificationService = new NotificationService();

        public MainForm()
        {
            InitializeFormLayout();
            this.Text = $"XBit - Logged in as {AuthService.CurrentUser.Name}";
            Theme.Apply(this);
            InternalNavigate(typeof(PageHome), null);
            UpdateBackButtonVisibility();
        }

        private void InitializeFormLayout()
        {
            this.Text = "XBit";
            this.Size = new Size(1200, 800);
            this.MinimumSize = new Size(800, 600);

            pnlContent = new Panel { Name = "pnlContent", Dock = DockStyle.Fill };
            pnlSidebar = new FlowLayoutPanel { Name = "pnlSidebar", Dock = DockStyle.Left, Width = 200, FlowDirection = FlowDirection.TopDown };
            pnlSidebar.BackColor = Theme.BgSidebar;

            btnBack = new Button { Text = "Back", Dock = DockStyle.Top, Height = 44 };
            btnBack.BackColor = Color.LightGray;
            btnBack.ForeColor = Color.Black;
            btnBack.FlatStyle = FlatStyle.Flat;
            btnBack.FlatAppearance.BorderSize = 1;
            btnBack.FlatAppearance.BorderColor = Color.DarkGray;
            btnBack.Click += BtnBack_Click;

            var btnHome = new Button { Text = "Home", Width = 180, Height = 44, Margin = new Padding(10, 5, 10, 5) };
            var btnAssignments = new Button { Text = "Assignments", Width = 180, Height = 44, Margin = new Padding(10, 5, 10, 5) };
            var btnBoard = new Button { Text = "Board", Width = 180, Height = 44, Margin = new Padding(10, 5, 10, 5) };
            var btnProjectBoard = new Button { Text = "Project Board", Width = 180, Height = 44, Margin = new Padding(10, 5, 10, 5) };
            var btnSettings = new Button { Text = "Settings", Width = 180, Height = 44, Margin = new Padding(10, 5, 10, 5) };
            var btnLogout = new Button { Text = "Logout", Width = 180, Height = 44, Margin = new Padding(10, 20, 10, 5) };

            Theme.StyleNavButton(btnHome);
            Theme.StyleNavButton(btnAssignments);
            Theme.StyleNavButton(btnBoard);
            Theme.StyleNavButton(btnProjectBoard);
            Theme.StyleNavButton(btnSettings);

            btnLogout.BackColor = Color.IndianRed;
            btnLogout.ForeColor = Color.White;
            btnLogout.FlatAppearance.MouseOverBackColor = Color.Firebrick;
            btnLogout.Click += BtnLogout_Click; // ⭐️ 대소문자 수정

            pnlSidebar.Controls.Add(btnBack);
            pnlSidebar.Controls.Add(btnHome);
            pnlSidebar.Controls.Add(btnAssignments);
            pnlSidebar.Controls.Add(btnBoard);
            pnlSidebar.Controls.Add(btnProjectBoard);
            pnlSidebar.Controls.Add(btnSettings);
            pnlSidebar.Controls.Add(btnLogout);

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlSidebar);

            btnHome.Click += (_, __) => NavigateTo<PageHome>();
            btnAssignments.Click += (_, __) => NavigateTo<PageAssignments>();
            btnBoard.Click += (_, __) => NavigateTo<PageBoard>();
            btnProjectBoard.Click += (_, __) => NavigateTo<PageProjectBoard>();
            btnSettings.Click += (_, __) => NavigateTo<PageSettings>();
        }

        private void BtnBack_Click(object sender, EventArgs e) => GoBack();

        // ⭐️ 메서드명 통일: BtnLogout_Click (대문자 B)
        private void BtnLogout_Click(object sender, EventArgs e)
        {
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

            btnBack.ForeColor = Color.Black;
        }

        private Panel CreateHeader()
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Theme.Primary
            };

            var lblTitle = new Label
            {
                Text = "XBIT",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };

            var lblUser = new Label
            {
                Text = $"{AuthService.CurrentUser.Name} 님",
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.White,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            btnNotifications = new Button
            {
                Text = "알림",
                Width = 80,
                Height = 35,
                ForeColor = Color.White,
                BackColor = Theme.Primary,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnNotifications.FlatAppearance.BorderColor = Color.White;
            btnNotifications.Click += BtnNotifications_Click;

            var btnLogout = new Button
            {
                Text = "로그아웃",
                Width = 80,
                Height = 35,
                ForeColor = Color.White,
                BackColor = Theme.Danger,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += BtnLogout_Click; // ⭐️ 대소문자 일치
                
            header.Resize += (s, e) =>
            {
                lblUser.Location = new Point(header.Width - 260, 20);
                btnNotifications.Location = new Point(header.Width - 180, 12);
                btnLogout.Location = new Point(header.Width - 95, 12);
            };

            header.Controls.Add(lblTitle);
            header.Controls.Add(lblUser);
            header.Controls.Add(btnNotifications);
            header.Controls.Add(btnLogout);

            var notificationTimer = new System.Windows.Forms.Timer();
            notificationTimer.Interval = 10000;
            notificationTimer.Tick += (s, e) => UpdateNotificationBadge();
            notificationTimer.Start();

            UpdateNotificationBadge();

            return header;
        }

        private void UpdateNotificationBadge()
        {
            if (AuthService.CurrentUser == null) return;

            int unreadCount = _notificationService.GetUnreadCount(AuthService.CurrentUser.Id);

            if (unreadCount > 0)
            {
                btnNotifications.Text = $"알림 ({unreadCount})";
                btnNotifications.BackColor = Color.FromArgb(244, 67, 54);
            }
            else
            {
                btnNotifications.Text = "알림";
                btnNotifications.BackColor = Theme.Primary;
            }
        }

        private void BtnNotifications_Click(object sender, EventArgs e)
        {
            NavigateTo<PageNotifications>();
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

            UserControl newPage;

            if (pageType == typeof(PageAssignmentDetail) && parameter is int assignmentId && assignmentId != -1)
            {
                newPage = (UserControl)Activator.CreateInstance(pageType, assignmentId);
            }
            else if (pageType == typeof(PagePostDetail) && parameter is int postId && postId != -1)
            {
                newPage = (UserControl)Activator.CreateInstance(pageType, postId);
            }
            else if (pageType == typeof(PageAssignments) && parameter is string filter)
            {
                newPage = (UserControl)Activator.CreateInstance(pageType);
                ((PageAssignments)newPage).FilterData(filter);
            }
            else if (pageType == typeof(PageSettings) && parameter is string sectionTag)
            {
                newPage = (UserControl)Activator.CreateInstance(pageType);
            }
            else
            {
                newPage = (UserControl)Activator.CreateInstance(pageType);
            }

            newPage.Padding = new Padding(0);
            newPage.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(newPage);

            CurrentPage = new NavigationEntry(pageType, parameter);

            UpdateBackButtonVisibility();
        }
    }
}