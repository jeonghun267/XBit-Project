// Pages/PageHome.cs (개선된 버전)

using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using XBit;
using XBit.Services;
using System;
using System.Linq;
using XBit.Models;
using System.Collections.Generic;
using System.Diagnostics;

namespace XBit.Pages
{
    public class PageHome : UserControl
    {
        private readonly AssignmentService _assignmentService = new AssignmentService();
        private readonly GitHubService _github_service = new GitHubService();
        private readonly NotificationService _notification_service = new NotificationService();
        private readonly BoardService _board_service = new BoardService();
        private readonly TaskService _task_service = new TaskService();
        private readonly StatisticsService _statisticsService = new StatisticsService();

        private FlowLayoutPanel wrap;
        private Panel pnlRecentActivity;
        private Panel pnlStats; // 통계 섹션 (차트 + 레이블)

        public PageHome()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgMain;

            InitializeLayout();
            LoadData();
            LoadStatisticsSectionAsync(); // 통계 로드 요청

            Theme.ThemeChanged += () =>
            {
                BackColor = Theme.BgMain;
                foreach (Control c in wrap.Controls)
                {
                    if (c is Panel p) p.BackColor = Theme.BgCard;
                }
                if (pnlStats != null) pnlStats.BackColor = Theme.BgCard;
                Invalidate(true);
            };

            this.Resize += (s, e) => AdjustCardSizes();
        }

        private void InitializeLayout()
        {
            var pnlTitle = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Theme.BgMain,
                Padding = new Padding(20, 15, 20, 15)
            };

            var lblWelcome = new Label
            {
                Text = $"안녕하세요, {AuthService.CurrentUser?.Name ?? "사용자"}님!",
                Font = new Font("맑은 고딕", 18f, FontStyle.Bold),
                ForeColor = Theme.FgDefault,
                AutoSize = true,
                Location = new Point(20, 15)
            };

            var lblSubtitle = new Label
            {
                Text = $"오늘의 할 일을 확인하세요 · {DateTime.Now:yyyy년 MM월 dd일 dddd}",
                Font = new Font("맑은 고딕", 11f),
                ForeColor = Theme.FgMuted,
                AutoSize = true,
                Location = new Point(20, 48)
            };

            pnlTitle.Controls.Add(lblWelcome);
            pnlTitle.Controls.Add(lblSubtitle);

            // 통계 섹션 생성 (차트 + 레이블)
            pnlStats = CreateStatsPanel();

            wrap = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(20),
                AutoScroll = true
            };
            wrap.Margin = new Padding(0);
            wrap.AutoSize = false;
            Theme.EnableDoubleBuffer(wrap);

            // 홈 카드들
            wrap.Controls.Add(MakeCard("오늘 마감 임박", "데이터 로드 중...", "card_due_today", Color.FromArgb(244, 67, 54)));
            wrap.Controls.Add(MakeCard("이번 주 프로젝트", "데이터 로드 중...", "card_due_week", Color.FromArgb(66, 133, 244)));
            wrap.Controls.Add(MakeCard("GitHub 상태", "데이터 로드 중...", "card_github", Color.FromArgb(76, 175, 80)));
            wrap.Controls.Add(MakeCard("최근 게시물", "데이터 로드 중...", "card_board", Color.FromArgb(156, 39, 176)));
            wrap.Controls.Add(MakeCard("진행 중인 작업", "데이터 로드 중...", "card_tasks", Color.FromArgb(0, 150, 136)));

            // 최근 활동 패널
            pnlRecentActivity = CreateRecentActivityPanel();
            wrap.Controls.Add(pnlRecentActivity);

            // 통계 패널 (차트 + 레이블)
            wrap.Controls.Add(pnlStats);

            Controls.Add(wrap);
            Controls.Add(pnlTitle);

            this.Load += (s, e) => AdjustCardSizes();
            wrap.Resize += (s, e) => AdjustCardSizes();
        }

        private Panel CreateStatsPanel()
        {
            var panel = new Panel
            {
                Width = 700,
                Height = 380,
                Margin = new Padding(10),
                BackColor = Theme.BgCard
            };
            Theme.StyleCard(panel);

            var lblTitle = new Label
            {
                Text = "통계 · 활동",
                Font = new Font("맑은 고딕", 13f, FontStyle.Bold),
                Location = new Point(20, 8),
                AutoSize = true,
                ForeColor = Theme.FgDefault
            };

            // 차트 전용 패널
            var chartPanel = new Panel
            {
                Location = new Point(20, 50),
                Width = panel.Width - 40,
                Height = 310,
                BackColor = Color.Transparent
            };

            panel.Controls.Add(lblTitle);
            panel.Controls.Add(chartPanel);

            panel.Tag = new Dictionary<string, Control>
            {
                { "chartPanel", chartPanel }
            };

            panel.Resize += (s, e) =>
            {
                try
                {
                    var dict = panel.Tag as Dictionary<string, Control>;
                    var cPanel = dict["chartPanel"] as Panel;

                    cPanel.Location = new Point(20, 50);
                    cPanel.Width = Math.Max(300, panel.Width - 40);
                    cPanel.Height = Math.Max(200, panel.Height - 70);

                    // 차트와 레이블 재배치
                    var donut = cPanel.Controls.OfType<Control>().FirstOrDefault(c2 => c2.Tag as string == "donut");
                    var trend = cPanel.Controls.OfType<Control>().FirstOrDefault(c2 => c2.Tag as string == "trend");
                    var donutInfo = cPanel.Controls.OfType<Label>().FirstOrDefault(l => l.Tag as string == "donutInfo");
                    var donutTitle = cPanel.Controls.OfType<Label>().FirstOrDefault(l => l.Tag as string == "donutTitle");
                    var trendTitle = cPanel.Controls.OfType<Label>().FirstOrDefault(l => l.Tag as string == "trendTitle");
                    var trendLabels = cPanel.Controls.OfType<Panel>().FirstOrDefault(p2 => p2.Tag as string == "trendLabels");

                    if (donut != null && trend != null)
                    {
                        int donutSize = Math.Min(240, Math.Max(120, cPanel.Width / 2 - 20));
                        // Theme.CreateDonutChart는 내부적으로 size+40 폭을 사용하므로 동일한 규칙 적용
                        donut.Width = donutSize + 40;
                        donut.Height = donutSize + 60;

                        int trendW = Math.Max(220, cPanel.Width - donut.Width - 40);
                        trend.Width = trendW;
                        trend.Height = Math.Max(160, cPanel.Height - 40); // leave space for labels

                        donut.Location = new Point((cPanel.Width / 2 - donut.Width) / 2, (cPanel.Height - donut.Height - 24) / 2);
                        trend.Location = new Point(cPanel.Width / 2 + (cPanel.Width / 2 - trend.Width) / 2, (cPanel.Height - trend.Height - 24) / 2);

                        if (donutTitle != null)
                        {
                            var ttSize = TextRenderer.MeasureText(donutTitle.Text, donutTitle.Font);
                            donutTitle.Location = new Point(donut.Left + (donut.Width - ttSize.Width) / 2, donut.Top - ttSize.Height - 6);
                            donutTitle.BringToFront();
                        }

                        if (donutInfo != null)
                        {
                            var textSize = TextRenderer.MeasureText(donutInfo.Text, donutInfo.Font);
                            donutInfo.Location = new Point(donut.Left + (donut.Width - textSize.Width) / 2, donut.Bottom + 6);
                            donutInfo.BringToFront();
                        }

                        if (trendTitle != null)
                        {
                            var tSize = TextRenderer.MeasureText(trendTitle.Text, trendTitle.Font);
                            trendTitle.Location = new Point(trend.Left + (trend.Width - tSize.Width) / 2, trend.Top - tSize.Height - 6);
                            trendTitle.BringToFront();
                        }

                        // 트렌드 하단 라벨(월) 위치/폭 재계산
                        if (trendLabels != null)
                        {
                            trendLabels.Location = new Point(trend.Left, trend.Bottom + 6);
                            trendLabels.Width = trend.Width;
                            int n = trendLabels.Controls.Count;
                            if (n > 0)
                            {
                                int w = Math.Max(40, trendLabels.Width / n);
                                foreach (Control c in trendLabels.Controls)
                                {
                                    c.Width = w;
                                    c.Height = trendLabels.Height;
                                }
                            }
                            trendLabels.BringToFront();
                        }
                    }
                }
                catch { /* 무시 */ }
            };

            return panel;
        }

        private async void LoadStatisticsSectionAsync()
        {
            if (pnlStats == null) return;

            try
            {
                var stats = await Task.Run(() => _statisticsService.GetUserStatistics(AuthService.CurrentUser.Id));

                var dict = pnlStats.Tag as Dictionary<string, Control>;
                var chartPanel = dict["chartPanel"] as Panel;

                chartPanel.Controls.Clear();

                // 도넛 차트 (완료율)
                int donutSize = Math.Min(240, Math.Max(120, chartPanel.Width / 2 - 20));
                int total = Math.Max(1, stats.TotalAssignments);

                // 도넛 위 제목 (예: "완료율") - 먼저 추가해서 위치 계산에 사용
                var lblDonutTitle = new Label
                {
                    Text = "완료율",
                    Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
                    ForeColor = Theme.FgPrimary,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Tag = "donutTitle"
                };
                chartPanel.Controls.Add(lblDonutTitle);

                var donut = Theme.CreateDonutChart(stats.CompletedAssignments, total, Theme.Success, "완료율", donutSize);
                donut.Tag = "donut";
                donut.Location = new Point((chartPanel.Width / 2 - donut.Width) / 2, (chartPanel.Height - donut.Height) / 2);
                chartPanel.Controls.Add(donut);

                // 도넛 아래 숫자 레이블 (예: 12/20 완료)
                var lblDonutInfo = new Label
                {
                    Text = $"{stats.CompletedAssignments}/{total} 완료",
                    Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
                    ForeColor = Theme.FgPrimary,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Tag = "donutInfo"
                };
                chartPanel.Controls.Add(lblDonutInfo);

                // 트렌드 라인 (월별 활동)
                int trendW = Math.Max(220, chartPanel.Width - donut.Width - 40);
                int trendH = Math.Max(160, chartPanel.Height - 20);
                var trend = Theme.CreateTrendLine(stats.MonthlyActivity ?? new int[] { 0, 0, 0, 0, 0, 0 }, Theme.Primary, trendW, trendH);
                trend.Tag = "trend";
                chartPanel.Controls.Add(trend);

                // 트렌드 위 제목 (작게)
                var lblTrendTitle = new Label
                {
                    Text = "최근 6개월 활동",
                    Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
                    ForeColor = Theme.FgPrimary,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Tag = "trendTitle"
                };
                chartPanel.Controls.Add(lblTrendTitle);

                // 즉시 위치 계산 및 전면으로 올리기
                var donutControl = chartPanel.Controls.OfType<Control>().FirstOrDefault(c => c.Tag as string == "donut");
                var trendControl = chartPanel.Controls.OfType<Control>().FirstOrDefault(c => c.Tag as string == "trend");
                var donutTitle = chartPanel.Controls.OfType<Label>().FirstOrDefault(l => l.Tag as string == "donutTitle");
                var donutInfoLabel = chartPanel.Controls.OfType<Label>().FirstOrDefault(l => l.Tag as string == "donutInfo");
                var trendTitleLabel = chartPanel.Controls.OfType<Label>().FirstOrDefault(l => l.Tag as string == "trendTitle");

                if (donutControl != null && trendControl != null)
                {
                    donutControl.Location = new Point((chartPanel.Width / 2 - donutControl.Width) / 2, (chartPanel.Height - donutControl.Height) / 2);
                    trendControl.Location = new Point(chartPanel.Width / 2 + (chartPanel.Width / 2 - trendControl.Width) / 2, (chartPanel.Height - trendControl.Height) / 2);

                    if (donutTitle != null)
                    {
                        var ttSize = TextRenderer.MeasureText(donutTitle.Text, donutTitle.Font);
                        donutTitle.Location = new Point(donutControl.Left + (donutControl.Width - ttSize.Width) / 2, Math.Max(6, donutControl.Top - ttSize.Height - 6));
                        donutTitle.BringToFront();
                    }

                    if (donutInfoLabel != null)
                    {
                        var infoSize = TextRenderer.MeasureText(donutInfoLabel.Text, donutInfoLabel.Font);
                        donutInfoLabel.Location = new Point(donutControl.Left + (donutControl.Width - infoSize.Width) / 2, donutControl.Bottom + 6);
                        donutInfoLabel.BringToFront();
                    }

                    if (trendTitleLabel != null)
                    {
                        var tSize2 = TextRenderer.MeasureText(trendTitleLabel.Text, trendTitleLabel.Font);
                        trendTitleLabel.Location = new Point(trendControl.Left + (trendControl.Width - tSize2.Width) / 2, Math.Max(6, trendControl.Top - tSize2.Height - 6));
                        trendTitleLabel.BringToFront();
                    }
                }

                // 툴팁 추가: 마우스 오버 시 설명 제공
                Theme.CreateToolTip(donut, $"완료된 프로젝트: {stats.CompletedAssignments} / 총 {total}");
                Theme.CreateToolTip(trend, "최근 6개월의 과제/게시물 활동 추이입니다.");

                chartPanel.Invalidate();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PageHome] LoadStatisticsSectionAsync 오류: {ex.Message}");
            }
        }

        // 이하 기존 카드/활동 로직 (변경 없음)
        private void AdjustCardSizes()
        {
            if (wrap == null) return;

            int available = Math.Max(400, wrap.ClientSize.Width - wrap.Padding.Left - wrap.Padding.Right);
            int approxItem = 360;
            int columns = Math.Max(1, available / approxItem);
            int spacing = 20;
            int itemWidth = (available - (columns + 1) * spacing) / columns;
            itemWidth = Math.Min(340, Math.Max(260, itemWidth));

            foreach (Control c in wrap.Controls)
            {
                if (c == pnlRecentActivity || c == pnlStats) continue;
                if (c is Panel panel && panel.Tag is string tag && tag.StartsWith("card_"))
                {
                    panel.Width = itemWidth;
                }
            }

            if (pnlStats != null)
            {
                if (wrap.ClientSize.Width < 800)
                {
                    pnlStats.Width = Math.Max(300, itemWidth * 2 + spacing);
                }
                else
                {
                    pnlStats.Width = 700;
                }
            }
        }

        private Panel MakeCard(string title, string subtitle, string tag, Color accentColor)
        {
            var card = new Panel
            {
                Width = 340,
                Height = 180,
                Margin = new Padding(10),
                Tag = tag,
                BackColor = Theme.BgCard,
                Cursor = Cursors.Hand
            };
            Theme.StyleCard(card);

            var accentBar = new Panel { Dock = DockStyle.Top, Height = 4, BackColor = accentColor, Margin = new Padding(0) };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("맑은 고딕", 13f, FontStyle.Bold),
                Location = new Point(20, 20),
                AutoSize = true,
                ForeColor = Theme.FgDefault
            };

            var lblSub = new Label
            {
                Text = subtitle,
                Font = new Font("맑은 고딕", 11f),
                Location = new Point(20, 55),
                AutoSize = false,
                Size = new Size(300, 60),
                Tag = "subtitle",
                ForeColor = Theme.FgMuted
            };

            var lblTime = new Label
            {
                Text = DateTime.Now.ToString("HH:mm"),
                Font = new Font("맑은 고딕", 9f, FontStyle.Italic),
                Location = new Point(20, 140),
                AutoSize = true,
                Tag = "last_update",
                ForeColor = Theme.FgMuted
            };

            card.Controls.Add(accentBar);
            card.Controls.Add(lblTitle);
            card.Controls.Add(lblSub);
            card.Controls.Add(lblTime);

            card.Click += (s, e) => Card_Click(tag);
            card.MouseEnter += (s, e) => card.BackColor = Theme.Hover;
            card.MouseLeave += (s, e) => card.BackColor = Theme.BgCard;

            return card;
        }

        private Panel CreateRecentActivityPanel()
        {
            var panel = new Panel
            {
                Width = 700,
                Height = 380,
                Margin = new Padding(10),
                BackColor = Theme.BgCard
            };
            Theme.StyleCard(panel);

            var lblTitle = new Label
            {
                Text = "📋 최근 활동",
                Font = new Font("맑은 고딕", 13f, FontStyle.Bold),
                Location = new Point(20, 15),
                AutoSize = true,
                ForeColor = Theme.FgDefault
            };

            var activityList = new FlowLayoutPanel
            {
                Location = new Point(20, 50),
                Width = 660,
                Height = 310,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Tag = "activity_list"
            };

            panel.Controls.Add(lblTitle);
            panel.Controls.Add(activityList);

            return panel;
        }

        public void LoadData()
        {
            try
            {
                if (AuthService.CurrentUser == null)
                {
                    MessageBox.Show("사용자 정보를 불러올 수 없습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var assignments = _assignmentService.GetAssignmentsForUser(AuthService.CurrentUser.Id);
                DateTime now = DateTime.Now;

                var dueTodayCount = assignments.Count(a =>
                    (a.DueDate - now).TotalHours <= 24 &&
                    (a.DueDate - now).TotalHours > 0 &&
                    a.Status != "제출 완료"
                );

                DateTime startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
                DateTime endOfWeek = startOfWeek.AddDays(7);
                var dueThisWeek = assignments.Where(a => a.DueDate >= startOfWeek && a.DueDate < endOfWeek).ToList();
                var submittedCount = dueThisWeek.Count(a => a.Status == "제출 완료");

                var githubUser = SettingsService.Current?.Integrations?.GitHubUser;
                int changedFiles = 0;
                try { changedFiles = _github_service.GetChangedFilesCount(); }
                catch { }

                int unreadNotifications = _notification_service.GetUnreadCount(AuthService.CurrentUser.Id);

                List<Post> recentPosts = new List<Post>();
                try
                {
                    var allPosts = _board_service.GetAllPosts();
                    recentPosts = allPosts.OrderByDescending(p => p.CreatedDate).Take(5).ToList();
                }
                catch { }

                int inProgressTasks = 0;
                try
                {
                    var teamService = new TeamService();
                    var userTeams = teamService.GetTeamsByUser(AuthService.CurrentUser.Id);
                    foreach (var team in userTeams)
                    {
                        var tasks = _task_service.GetTasksByTeam(team.Id);
                        // 상태 문자열 불일치(한영) 대비: "진행 중" 또는 "InProgress" 등으로 판단
                        inProgressTasks += tasks.Count(t =>
                            string.Equals(t.Status, "InProgress", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(t.Status, "진행 중", StringComparison.OrdinalIgnoreCase)
                            || (t.Status != null && t.Status.IndexOf("진행", StringComparison.OrdinalIgnoreCase) >= 0)
                        );
                    }
                }
                catch { }

                UpdateCardText("card_due_today", dueTodayCount > 0 ? $"{dueTodayCount}개의 프로젝트\n24시간 이내 마감!" : "마감 임박 프로젝트 없음\n여유롭게 시작하세요");
                UpdateCardText("card_due_week", $"총 {dueThisWeek.Count}개 프로젝트\n제출: {submittedCount} / 미제출: {dueThisWeek.Count - submittedCount}");

                if (!string.IsNullOrWhiteSpace(githubUser) && !string.IsNullOrWhiteSpace(SettingsService.Current?.Integrations?.GitHubToken))
                {
                    UpdateCardText("card_github", changedFiles > 0 ? $"변경된 파일 {changedFiles}개\n클릭하여 동기화" : "모든 변경사항 동기화됨\n최신 상태입니다");
                }
                else
                {
                    UpdateCardText("card_github", "GitHub 연동 필요\n설정에서 토큰을 입력하세요");
                }

                UpdateCardText("card_board", recentPosts.Count > 0 ? $"최근 게시물 {recentPosts.Count}개\n클릭하여 확인하세요" : "새로운 게시물이 없습니다\n게시판을 확인해보세요");
                UpdateCardText("card_tasks", inProgressTasks > 0 ? $"진행 중인 작업 {inProgressTasks}개\n완료까지 힘내세요!" : "진행 중인 작업이 없습니다\n새로운 작업을 시작하세요");

                UpdateAllTimeLabels();
                LoadRecentActivity();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PageHome] LoadData 오류: {ex.Message}");
                MessageBox.Show($"데이터 로드 실패:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadRecentActivity()
        {
            var activityList = pnlRecentActivity.Controls.OfType<FlowLayoutPanel>().FirstOrDefault(p => (p.Tag as string) == "activity_list");
            if (activityList == null) return;

            activityList.Controls.Clear();

            try
            {
                var recentAssignments = _assignmentService.GetAssignmentsForUser(AuthService.CurrentUser.Id)
                    .Where(a => a.Status == "제출 완료")
                    .OrderByDescending(a => a.DueDate)
                    .Take(3);

                foreach (var assignment in recentAssignments)
                {
                    activityList.Controls.Add(CreateActivityItem("✅ 과제 제출 완료", assignment.Title, assignment.DueDate.ToString("yyyy-MM-dd HH:mm")));
                }

                try
                {
                    var allPosts = _board_service.GetAllPosts();
                    var recentPosts = allPosts.Where(p => p.AuthorId == AuthService.CurrentUser.Id).OrderByDescending(p => p.CreatedDate).Take(3);
                    foreach (var post in recentPosts)
                    {
                        activityList.Controls.Add(CreateActivityItem("📝 게시물 작성", post.Title, post.CreatedDate.ToString("yyyy-MM-dd HH:mm")));
                    }
                }
                catch { }

                if (activityList.Controls.Count == 0)
                {
                    var lblEmpty = new Label { Text = "최근 활동이 없습니다.", Font = new Font("맑은 고딕", 10f), ForeColor = Theme.FgMuted, AutoSize = true, Padding = new Padding(10) };
                    activityList.Controls.Add(lblEmpty);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PageHome] LoadRecentActivity 오류: {ex.Message}");
            }
        }

        private Panel CreateActivityItem(string type, string title, string time)
        {
            var item = new Panel { Width = 640, Height = 50, BackColor = Theme.BgMain, Margin = new Padding(0, 5, 0, 5), Padding = new Padding(10) };

            var lblType = new Label { Text = type, Font = new Font("맑은 고딕", 9f, FontStyle.Bold), ForeColor = Theme.AccentColor, AutoSize = true, Location = new Point(10, 5) };
            var lblTitle = new Label { Text = title, Font = new Font("맑은 고딕", 10f), ForeColor = Theme.FgDefault, AutoSize = false, Size = new Size(500, 20), Location = new Point(10, 22) };
            var lblTime = new Label { Text = time, Font = new Font("맑은 고딕", 8f), ForeColor = Theme.FgMuted, AutoSize = true, Location = new Point(520, 25) };

            item.Controls.Add(lblType);
            item.Controls.Add(lblTitle);
            item.Controls.Add(lblTime);

            return item;
        }

        private void UpdateCardText(string cardTag, string newText)
        {
            var card = wrap.Controls.OfType<Panel>().FirstOrDefault(p => (p.Tag as string) == cardTag);
            if (card != null)
            {
                var lblSub = card.Controls.OfType<Label>().FirstOrDefault(l => (l.Tag as string) == "subtitle");
                if (lblSub != null) lblSub.Text = newText;
            }
        }

        private void UpdateAllTimeLabels()
        {
            string currentTime = DateTime.Now.ToString("HH:mm");
            foreach (Control c in wrap.Controls)
            {
                if (c is Panel card && card != pnlRecentActivity)
                {
                    var lblTime = card.Controls.OfType<Label>().FirstOrDefault(l => (l.Tag as string) == "last_update");
                    if (lblTime != null) lblTime.Text = currentTime;
                }
            }
        }

        private void Card_Click(string cardTag)
        {
            var mainForm = FindForm() as MainForm;
            if (mainForm == null) return;

            switch (cardTag)
            {
                case "card_due_today": mainForm.NavigateTo<PageAssignments>("DueToday"); break;
                case "card_due_week": mainForm.NavigateTo<PageAssignments>("DueThisWeek"); break;
                case "card_github":
                    var githubToken = SettingsService.Current?.Integrations?.GitHubToken;
                    if (string.IsNullOrWhiteSpace(githubToken)) mainForm.NavigateTo<PageSettings>("Integrations");
                    else ShowGitHubSyncDialog();
                    break;
                case "card_notifications": mainForm.NavigateTo<PageNotifications>(); break;
                case "card_board": mainForm.NavigateTo<PageBoard>(); break;
                case "card_tasks": mainForm.NavigateTo<PageProjectBoard>(); break;
            }
        }

        private async void ShowGitHubSyncDialog()
        {
            int changedFiles = _github_service.GetChangedFilesCount();
            if (changedFiles == 0) { MessageBox.Show("변경된 파일이 없습니다!", "정보", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            var result = MessageBox.Show($"변경된 파일 {changedFiles}개를\nGitHub에 업로드하시겠습니까?", "GitHub 동기화", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                bool success = await _github_service.SyncAllChanges();
                if (success)
                {
                    MessageBox.Show($"{changedFiles}개 파일 업로드 완료!", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadData();
                    var mainForm = FindForm() as MainForm;
                    if (mainForm != null) mainForm.UpdateSyncStatus();
                }
                else { MessageBox.Show("동기화 실패!\n토큰과 권한을 확인하세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
    }
}