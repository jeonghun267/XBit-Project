// Pages/PageHome.cs (개선된 버전)

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
        private readonly GitHubService _githubService = new GitHubService();
        private readonly NotificationService _notificationService = new NotificationService();
        private readonly BoardService _boardService = new BoardService();
        private readonly TaskService _taskService = new TaskService();
        private FlowLayoutPanel wrap;
        private Panel pnlRecentActivity;

        public PageHome()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgMain;

            InitializeLayout();
            LoadData();

            Theme.ThemeChanged += () =>
            {
                BackColor = Theme.BgMain;
                foreach (Control c in wrap.Controls)
                {
                    if (c is Panel p) p.BackColor = Theme.BgCard;
                }
                Invalidate(true);
            };
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

            wrap = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(20),
                AutoScroll = true
            };
            Theme.EnableDoubleBuffer(wrap);

            // 카드들 추가
            wrap.Controls.Add(MakeCard(
                "오늘 마감 임박", 
                "데이터 로드 중...", 
                "card_due_today", 
                Color.FromArgb(244, 67, 54)
            ));
            
            wrap.Controls.Add(MakeCard(
                "이번 주 프로젝트", 
                "데이터 로드 중...", 
                "card_due_week", 
                Color.FromArgb(66, 133, 244)
            ));
            
            wrap.Controls.Add(MakeCard(
                "GitHub 상태", 
                "데이터 로드 중...", 
                "card_github", 
                Color.FromArgb(76, 175, 80)
            ));

            wrap.Controls.Add(MakeCard(
                "알림", 
                "데이터 로드 중...", 
                "card_notifications", 
                Color.FromArgb(255, 152, 0)
            ));

            wrap.Controls.Add(MakeCard(
                "최근 게시물", 
                "데이터 로드 중...", 
                "card_board", 
                Color.FromArgb(156, 39, 176)
            ));

            wrap.Controls.Add(MakeCard(
                "진행 중인 작업", 
                "데이터 로드 중...", 
                "card_tasks", 
                Color.FromArgb(0, 150, 136)
            ));

            // 최근 활동 패널 추가
            pnlRecentActivity = CreateRecentActivityPanel();
            wrap.Controls.Add(pnlRecentActivity);

            Controls.Add(wrap);
            Controls.Add(pnlTitle);
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

            var accentBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 4,
                BackColor = accentColor,
                Margin = new Padding(0)
            };

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

        private void LoadData()
        {
            try
            {
                if (AuthService.CurrentUser == null)
                {
                    MessageBox.Show("사용자 정보를 불러올 수 없습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var assignments = _assignmentService.GetAssignmentsForUser(AuthService.CurrentUser.Id);
                
                System.Diagnostics.Debug.WriteLine($"[PageHome] 프로젝트 수: {assignments.Count}");
                
                DateTime now = DateTime.Now;

                // 오늘 마감 임박
                var dueTodayCount = assignments.Count(a => 
                    (a.DueDate - now).TotalHours <= 24 && 
                    (a.DueDate - now).TotalHours > 0 && 
                    a.Status != "제출 완료"
                );

                // 이번 주 프로젝트
                DateTime startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
                DateTime endOfWeek = startOfWeek.AddDays(7);
                var dueThisWeek = assignments.Where(a => a.DueDate >= startOfWeek && a.DueDate < endOfWeek).ToList();
                var submittedCount = dueThisWeek.Count(a => a.Status == "제출 완료");

                // GitHub 상태
                var githubUser = SettingsService.Current?.Integrations?.GitHubUser;
                int changedFiles = 0;
                try
                {
                    changedFiles = _githubService.GetChangedFilesCount();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PageHome] GitHub 상태 확인 실패: {ex.Message}");
                }

                // 알림
                int unreadNotifications = _notificationService.GetUnreadCount(AuthService.CurrentUser.Id);

                // 최근 게시물
                List<Post> recentPosts = new List<Post>();
                try
                {
                    var allPosts = _boardService.GetAllPosts();
                    recentPosts = allPosts.OrderByDescending(p => p.CreatedDate).Take(5).ToList();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PageHome] 게시물 로드 실패: {ex.Message}");
                }

                // 진행 중인 작업
                int inProgressTasks = 0;
                try
                {
                    // TeamService를 통해 사용자의 팀을 가져온 후 해당 팀의 작업들을 확인
                    var teamService = new TeamService();
                    var userTeams = teamService.GetTeamsByUser(AuthService.CurrentUser.Id);
                    foreach (var team in userTeams)
                    {
                        var tasks = _taskService.GetTasksByTeam(team.Id);
                        inProgressTasks += tasks.Count(t => t.Status == "InProgress");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PageHome] 작업 로드 실패: {ex.Message}");
                }

                // 카드 업데이트
                UpdateCardText("card_due_today", dueTodayCount > 0 
                    ? $"{dueTodayCount}개의 프로젝트\n24시간 이내 마감!" 
                    : "마감 임박 프로젝트 없음\n여유롭게 시작하세요");

                UpdateCardText("card_due_week", 
                    $"총 {dueThisWeek.Count}개 프로젝트\n제출: {submittedCount} / 미제출: {dueThisWeek.Count - submittedCount}");

                if (!string.IsNullOrWhiteSpace(githubUser) && 
                    !string.IsNullOrWhiteSpace(SettingsService.Current?.Integrations?.GitHubToken))
                {
                    UpdateCardText("card_github", changedFiles > 0 
                        ? $"변경된 파일 {changedFiles}개\n클릭하여 동기화" 
                        : "모든 변경사항 동기화됨\n최신 상태입니다");
                }
                else
                {
                    UpdateCardText("card_github", "GitHub 연동 필요\n설정에서 토큰을 입력하세요");
                }

                UpdateCardText("card_notifications", unreadNotifications > 0
                    ? $"읽지 않은 알림 {unreadNotifications}개\n확인이 필요합니다"
                    : "모든 알림 확인 완료\n새로운 알림이 없습니다");

                UpdateCardText("card_board", recentPosts.Count > 0
                    ? $"최근 게시물 {recentPosts.Count}개\n클릭하여 확인하세요"
                    : "새로운 게시물이 없습니다\n게시판을 확인해보세요");

                UpdateCardText("card_tasks", inProgressTasks > 0
                    ? $"진행 중인 작업 {inProgressTasks}개\n완료까지 힘내세요!"
                    : "진행 중인 작업이 없습니다\n새로운 작업을 시작하세요");

                UpdateAllTimeLabels();
                LoadRecentActivity();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PageHome] LoadData 오류: {ex.Message}");
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
                // 최근 제출한 과제
                var recentAssignments = _assignmentService.GetAssignmentsForUser(AuthService.CurrentUser.Id)
                    .Where(a => a.Status == "제출 완료")
                    .OrderByDescending(a => a.DueDate)
                    .Take(3);

                foreach (var assignment in recentAssignments)
                {
                    activityList.Controls.Add(CreateActivityItem(
                        "✅ 과제 제출 완료",
                        assignment.Title,
                        assignment.DueDate.ToString("yyyy-MM-dd HH:mm")
                    ));
                }

                // 최근 게시물
                try
                {
                    var allPosts = _boardService.GetAllPosts();
                    var recentPosts = allPosts
                        .Where(p => p.AuthorId == AuthService.CurrentUser.Id)
                        .OrderByDescending(p => p.CreatedDate)
                        .Take(3);

                    foreach (var post in recentPosts)
                    {
                        activityList.Controls.Add(CreateActivityItem(
                            "📝 게시물 작성",
                            post.Title,
                            post.CreatedDate.ToString("yyyy-MM-dd HH:mm")
                        ));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PageHome] 최근 게시물 로드 실패: {ex.Message}");
                }

                if (activityList.Controls.Count == 0)
                {
                    var lblEmpty = new Label
                    {
                        Text = "최근 활동이 없습니다.",
                        Font = new Font("맑은 고딕", 10f),
                        ForeColor = Theme.FgMuted,
                        AutoSize = true,
                        Padding = new Padding(10)
                    };
                    activityList.Controls.Add(lblEmpty);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PageHome] LoadRecentActivity 오류: {ex.Message}");
            }
        }

        private Panel CreateActivityItem(string type, string title, string time)
        {
            var item = new Panel
            {
                Width = 640,
                Height = 50,
                BackColor = Theme.BgMain,
                Margin = new Padding(0, 5, 0, 5),
                Padding = new Padding(10)
            };

            var lblType = new Label
            {
                Text = type,
                Font = new Font("맑은 고딕", 9f, FontStyle.Bold),
                ForeColor = Theme.AccentColor,
                AutoSize = true,
                Location = new Point(10, 5)
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("맑은 고딕", 10f),
                ForeColor = Theme.FgDefault,
                AutoSize = false,
                Size = new Size(500, 20),
                Location = new Point(10, 22)
            };

            var lblTime = new Label
            {
                Text = time,
                Font = new Font("맑은 고딕", 8f),
                ForeColor = Theme.FgMuted,
                AutoSize = true,
                Location = new Point(520, 25)
            };

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
                if (lblSub != null)
                {
                    lblSub.Text = newText;
                }
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
                    if (lblTime != null)
                    {
                        lblTime.Text = currentTime;
                    }
                }
            }
        }

        private void Card_Click(string cardTag)
        {
            var mainForm = FindForm() as MainForm;
            if (mainForm == null) return;

            switch (cardTag)
            {
                case "card_due_today":
                    mainForm.NavigateTo<PageAssignments>("DueToday");
                    break;

                case "card_due_week":
                    mainForm.NavigateTo<PageAssignments>("DueThisWeek");
                    break;

                case "card_github":
                    var githubToken = SettingsService.Current?.Integrations?.GitHubToken;

                    if (string.IsNullOrWhiteSpace(githubToken))
                    {
                        mainForm.NavigateTo<PageSettings>("Integrations");
                    }
                    else
                    {
                        ShowGitHubSyncDialog();
                    }
                    break;

                case "card_notifications":
                    mainForm.NavigateTo<PageNotifications>();
                    break;

                case "card_board":
                    mainForm.NavigateTo<PageBoard>();
                    break;

                case "card_tasks":
                    mainForm.NavigateTo<PageProjectBoard>();
                    break;
            }
        }

        private async void ShowGitHubSyncDialog()
        {
            int changedFiles = _githubService.GetChangedFilesCount();

            if (changedFiles == 0)
            {
                MessageBox.Show("변경된 파일이 없습니다!", "정보", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"변경된 파일 {changedFiles}개를\nGitHub에 업로드하시겠습니까?",
                "GitHub 동기화",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                bool success = await _githubService.SyncAllChanges();

                if (success)
                {
                    MessageBox.Show($"{changedFiles}개 파일 업로드 완료!", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadData();
                }
                else
                {
                    MessageBox.Show("동기화 실패!\n토큰과 권한을 확인하세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}