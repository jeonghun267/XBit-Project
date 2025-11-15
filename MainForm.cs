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

        public MainForm()
        {
            InitializeFormLayout();
            this.Text = $"XBit - Logged in as {AuthService.CurrentUser.Name}";
            Theme.Apply(this);
            InternalNavigate(typeof(PageHome), null);
            UpdateBackButtonVisibility(); // 초기 상태 설정
        }

        private void InitializeFormLayout()
        {
            this.Text = "XBit";
            this.Size = new Size(1200, 800);
            this.MinimumSize = new Size(800, 600);

            pnlContent = new Panel { Name = "pnlContent", Dock = DockStyle.Fill };
            pnlSidebar = new FlowLayoutPanel { Name = "pnlSidebar", Dock = DockStyle.Left, Width = 200, FlowDirection = FlowDirection.TopDown };
            pnlSidebar.BackColor = Theme.BgSidebar;

            // 뒤로가기 버튼
            btnBack = new Button { Text = "Back", Dock = DockStyle.Top, Height = 44 };
            btnBack.BackColor = Color.LightGray;
            btnBack.ForeColor = Color.Black;
            btnBack.FlatStyle = FlatStyle.Flat;
            btnBack.FlatAppearance.BorderSize = 1;
            btnBack.FlatAppearance.BorderColor = Color.DarkGray;
            btnBack.Click += BtnBack_Click;

            // 메뉴 버튼 생성 및 스타일 적용
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

            // 로그아웃 버튼 스타일
            btnLogout.BackColor = Color.IndianRed;
            btnLogout.ForeColor = Color.White;
            btnLogout.FlatAppearance.MouseOverBackColor = Color.Firebrick;
            btnLogout.Click += btnLogout_Click;

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

        private void btnLogout_Click(object sender, EventArgs e)
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

        // ⭐️ 이 메서드를 수정하여 글씨 색상을 강제로 유지
        private void UpdateBackButtonVisibility()
        {
            bool isEnabled = NavigationStack.Count > 0;

            btnBack.Enabled = isEnabled;
            btnBack.Visible = true;

            // ⭐️ 핵심 수정: 버튼의 상태와 관계없이 글씨색을 검검으로 강제 지정하여 흐릿해지는 것을 방지
            btnBack.ForeColor = Color.Black;
        }

        // ... (NavigateTo 및 InternalNavigate 메서드 유지) ...
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
            // ⭐️ 이전 페이지 완전 정리
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

            // ⭐️ Padding 명시적 초기화 및 Dock
            newPage.Padding = new Padding(0);
            newPage.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(newPage);

            CurrentPage = new NavigationEntry(pageType, parameter);

            UpdateBackButtonVisibility();
        }
    }
}