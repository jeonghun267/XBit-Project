// Pages/PageHome.cs (중복 메서드 제거, 통계 로직 유지)

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

                int donutSize = Math.Min(240, Math.Max(120, chartPanel.Width / 2 - 20));

                int totalTasks = 0;
                int completedTasks = 0;
                try
                {
                    var teamService = new TeamService();
                    var userTeams = teamService.GetTeamsByUser(AuthService.CurrentUser.Id);
                    foreach (var team in userTeams)
                    {
                        var tasks = _task_service.GetTasksByTeam(team.Id);
                        if (tasks == null) continue;

                        totalTasks += tasks.Count;
                        completedTasks += tasks.Count(t =>
                            string.Equals(t.Status, "완료", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(t.Status, "Done", StringComparison.OrdinalIgnoreCase)
                            || (t.Status != null && t.Status.IndexOf("완료", StringComparison.OrdinalIgnoreCase) >= 0)
                        );
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PageHome] Task counts load error: {ex.Message}");
                    totalTasks = Math.Max(1, stats?.TotalAssignments ?? 1);
                    completedTasks = stats?.CompletedAssignments ?? 0;
                }

                int donutTotal = Math.Max(1, totalTasks);
                int donutCompleted = Math.Max(0, completedTasks);

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

                var donut = Theme.CreateDonutChart(donutCompleted, donutTotal, Theme.Success, "완료율", donutSize);
                donut.Tag = "donut";
                donut.Location = new Point((chartPanel.Width / 2 - donut.Width) / 2, (chartPanel.Height - donut.Height) / 2);
                chartPanel.Controls.Add(donut);

                var lblDonutInfo = new Label
                {
                    Text = $"{donutCompleted}/{donutTotal} 완료",
                    Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
                    ForeColor = Theme.FgPrimary,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Tag = "donutInfo"
                };
                chartPanel.Controls.Add(lblDonutInfo);

                // --- 기존 트렌드(꺾은선) 대신: 이번 달 출석 캘린더 로드 ---
                try
                {
                    int year = DateTime.Now.Year;
                    int month = DateTime.Now.Month;
                    var activeDates = new List<DateTime>();

                    // 게시물로부터 활동일 수집 (작성일)
                    try
                    {
                        var allPosts = _board_service.GetAllPosts();
                        if (allPosts != null)
                        {
                            foreach (var p in allPosts)
                            {
                                if (p.AuthorId == AuthService.CurrentUser.Id && p.CreatedDate.Year == year && p.CreatedDate.Month == month)
                                {
                                    activeDates.Add(p.CreatedDate.Date);
                                }
                            }
                        }
                    }
                    catch { /* 게시물 수집 실패 무시 */ }

                    // 제출된 과제(대체 데이터)로부터 활동일 수집 (DueDate 기준)
                    try
                    {
                        var assignments = _assignmentService.GetAssignmentsForUser(AuthService.CurrentUser.Id);
                        if (assignments != null)
                        {
                            foreach (var a in assignments)
                            {
                                if (string.Equals(a.Status, "제출 완료", StringComparison.OrdinalIgnoreCase)
                                    && a.DueDate.Year == year && a.DueDate.Month == month)
                                {
                                    activeDates.Add(a.DueDate.Date);
                                }
                            }
                        }
                    }
                    catch { /* 과제 수집 실패 무시 */ }

                    // 중복 제거
                    activeDates = activeDates.Select(d => d.Date).Distinct().ToList();

                    // 캘린더 로드
                    LoadAttendanceCalendar(year, month, activeDates);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PageHome] Attendance calendar build failed: {ex.Message}");
                }

                // 도넛 툴팁
                Theme.CreateToolTip(donut, $"완료된 작업: {donutCompleted} / 총 작업: {donutTotal}");

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
        // PageHome: 출석 캘린더 로더
        private void LoadAttendanceCalendar(int year, int month, List<DateTime> activeDates)
        {
            if (pnlStats == null) return;
            var dict = pnlStats.Tag as Dictionary<string, Control>;
            if (dict == null || !dict.ContainsKey("chartPanel")) return;
            var chartPanel = dict["chartPanel"] as Panel;
            if (chartPanel == null) return;

            // 이전 캘린더 제거 (재호출 대비)
            var existing = chartPanel.Controls.OfType<Control>().FirstOrDefault(c => (c.Tag as string) == "attendanceCalendar");
            if (existing != null) chartPanel.Controls.Remove(existing);

            // 캘린더 크기/위치 산정 (donut 오른쪽에 배치)
            var donutControl = chartPanel.Controls.OfType<Control>().FirstOrDefault(c => (c.Tag as string) == "donut");
            int calWidth = donutControl != null ? Math.Max(220, chartPanel.Width - donutControl.Width - 40) : Math.Max(300, chartPanel.Width - 40);
            int calHeight = Math.Max(160, chartPanel.Height - 20);

            var cal = new FlowLayoutPanel
            {
                Tag = "attendanceCalendar",
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Width = calWidth,
                Height = calHeight,
                BackColor = Color.Transparent,
                Padding = new Padding(2),
                Margin = new Padding(0)
            };

            // 위치 설정: donut 오른쪽이면 오른쪽 중앙, 없으면 가운데
            if (donutControl != null)
            {
                cal.Location = new Point(chartPanel.Width / 2 + (chartPanel.Width / 2 - cal.Width) / 2, (chartPanel.Height - cal.Height) / 2);
            }
            else
            {
                cal.Location = new Point((chartPanel.Width - cal.Width) / 2, (chartPanel.Height - cal.Height) / 2);
            }

            // 요일 헤더 (일~토)
            string[] dayNames = new[] { "일", "월", "화", "수", "목", "금", "토" };
            int cellSize = Math.Max(28, Math.Min(46, calWidth / 8));
            foreach (var dn in dayNames)
            {
                var lbl = new Label
                {
                    Text = dn,
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Width = cellSize,
                    Height = 20,
                    ForeColor = Theme.FgMuted,
                    BackColor = Color.Transparent,
                    Margin = new Padding(2)
                };
                cal.Controls.Add(lbl);
            }

            // 달력 시작 오프셋(1일의 요일)
            var firstOfMonth = new DateTime(year, month, 1);
            int offset = (int)firstOfMonth.DayOfWeek; // Sunday=0 ... Saturday=6

            // 빈칸 채우기
            for (int i = 0; i < offset; i++)
            {
                var placeholder = new Panel
                {
                    Width = cellSize,
                    Height = cellSize,
                    BackColor = Color.Transparent,
                    Margin = new Padding(2)
                };
                cal.Controls.Add(placeholder);
            }

            // activeDates 집합(날짜 비교는 Date 부분만)
            var activeSet = new HashSet<DateTime>((activeDates ?? new List<DateTime>()).Select(d => d.Date));

            // 날짜 버튼 생성
            int daysInMonth = DateTime.DaysInMonth(year, month);
            for (int d = 1; d <= daysInMonth; d++)
            {
                var dt = new DateTime(year, month, d);
                bool isActive = activeSet.Contains(dt.Date);

                var btn = new Button
                {
                    Text = d.ToString(),
                    Width = cellSize,
                    Height = cellSize,
                    BackColor = isActive ? Color.SeaGreen : Color.FromArgb(40, 40, 40),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Margin = new Padding(2),
                    Tag = dt.Date,
                    Enabled = false // 클릭 동작이 필요하면 true로 바꿔 핸들러 추가
                };
                // 모던 스타일: 테두리 제거
                btn.FlatAppearance.BorderSize = 0;

                cal.Controls.Add(btn);
            }

            // 캘린더를 차트 패널에 추가
            chartPanel.Controls.Add(cal);
            cal.BringToFront();
        }
    }
}