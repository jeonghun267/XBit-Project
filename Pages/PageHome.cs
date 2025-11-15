// Pages/PageHome.cs (중복 변수 제거)

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
        private FlowLayoutPanel wrap;
        private Button btnRefresh;

        public PageHome()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgMain;

            InitializeLayout();
            LoadData();

            // 테마 변경 시 카드 스타일 업데이트
            Theme.ThemeChanged += () =>
            {
                BackColor = Theme.BgMain;
                foreach (Control c in wrap.Controls)
                {
                    if (c is Panel p) p.BackColor = Theme.BgCard;
                    if (c is Button b && b == btnRefresh)
                    {
                        Theme.StylePrimaryButton(b);
                    }
                    foreach (Control cc in c.Controls)
                        if (cc is Label l)
                            l.ForeColor = (l.Tag as string) == "muted" ? Theme.FgMuted : Theme.FgDefault;
                }
                Invalidate(true);
            };
        }

        private void InitializeLayout()
        {
            // 상단 헤더 패널
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Theme.BgMain,
                Padding = new Padding(8)
            };

            var lblPageTitle = new Label
            {
                Text = "📊 대시보드",
                Font = new Font("맑은 고딕", 16f, FontStyle.Bold),
                ForeColor = Theme.FgDefault,
                AutoSize = true,
                Location = new Point(15, 15)
            };

            btnRefresh = new Button
            {
                Text = "🔄 새로고침",
                Width = 120,
                Height = 35,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            pnlHeader.Resize += (s, e) =>
            {
                btnRefresh.Location = new Point(pnlHeader.Width - 140, 12);
            };
            btnRefresh.Location = new Point(pnlHeader.Width - 140, 12);

            Theme.StylePrimaryButton(btnRefresh);
            btnRefresh.Click += BtnRefresh_Click;

            pnlHeader.Controls.Add(lblPageTitle);
            pnlHeader.Controls.Add(btnRefresh);

            // 카드 컨테이너
            wrap = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(8),
                AutoScroll = true
            };
            Theme.EnableDoubleBuffer(wrap);

            // 카드 초기 생성
            wrap.Controls.Add(MakeCard("오늘 마감 임박", "데이터 로드 중...", "card_due_today"));
            wrap.Controls.Add(MakeCard("이번 주 과제", "데이터 로드 중...", "card_due_week"));
            wrap.Controls.Add(MakeCard("깃허브 알림", "데이터 로드 중...", "card_github"));

            Controls.Add(pnlHeader);
            Controls.Add(wrap);
        }

        private Panel MakeCard(string title, string subtitle, string tag)
        {
            var card = new Panel
            {
                Width = 280,
                Height = 160,
                Margin = new Padding(8),
                Tag = tag,
                BackColor = Theme.BgCard,
                Cursor = Cursors.Hand,
                BorderStyle = BorderStyle.FixedSingle,
                ForeColor = Theme.Border
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            };
            Theme.StyleTitle(lblTitle);
            lblTitle.Height = 28;

            var lblSub = new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                Location = new Point(10, 45),
                AutoSize = true,
                Tag = "subtitle"
            };
            Theme.StyleMuted(lblSub);

            var lblLastUpdate = new Label
            {
                Text = "업데이트: " + DateTime.Now.ToString("HH:mm:ss"),
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                Location = new Point(10, 130),
                AutoSize = true,
                Tag = "last_update",
                ForeColor = Theme.FgMuted
            };

            card.Controls.Add(lblLastUpdate);
            card.Controls.Add(lblSub);
            card.Controls.Add(lblTitle);
            card.Controls.SetChildIndex(lblTitle, 0);

            card.Click += (sender, e) => Card_Click(tag);
            foreach (Control c in card.Controls)
            {
                c.Click += (sender, e) => Card_Click(tag);
            }

            return card;
        }

        // ⭐️ 새로고침 버튼: GitHub 자동 동기화 추가
        private async void BtnRefresh_Click(object sender, EventArgs e)
        {
            btnRefresh.Enabled = false;
            btnRefresh.Text = "⏳ 동기화 중...";

            try
            {
                // 1. 로컬 데이터 로드
                LoadData();

                // 2. GitHub에 변경사항이 있는지 확인
                int changedFiles = _githubService.GetChangedFilesCount();

                if (changedFiles > 0)
                {
                    var result = MessageBox.Show(
                        $"변경된 파일 {changedFiles}개를 GitHub에 업로드하시겠습니까?",
                        "GitHub 동기화",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );

                    if (result == DialogResult.Yes)
                    {
                        btnRefresh.Text = "⏳ GitHub 푸시 중...";
                        
                        bool success = await _githubService.SyncAllChanges();

                        if (success)
                        {
                            MessageBox.Show(
                                $"✅ {changedFiles}개 파일이 GitHub에 업로드되었습니다!",
                                "동기화 완료",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information
                            );
                        }
                        else
                        {
                            MessageBox.Show(
                                "GitHub 푸시 실패. 토큰과 권한을 확인하세요.",
                                "오류",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error
                            );
                        }
                    }
                }
                else
                {
                    MessageBox.Show("변경된 파일이 없습니다.", "정보", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                // 3. 카드 업데이트 시간 갱신
                foreach (Control c in wrap.Controls)
                {
                    if (c is Panel card)
                    {
                        var lblLastUpdate = card.Controls.OfType<Label>().FirstOrDefault(l => (l.Tag as string) == "last_update");
                        if (lblLastUpdate != null)
                        {
                            lblLastUpdate.Text = "업데이트: " + DateTime.Now.ToString("HH:mm:ss");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRefresh.Enabled = true;
                btnRefresh.Text = "🔄 새로고침";
            }
        }

        private void LoadData()
        {
            // ⭐️ 변수 재선언 제거 - 한 번만 선언
            List<Assignment> assignments = new List<Assignment>()
            {
                new Assignment { Id = 1, Course = "C# WinForms", Title = "UI 구현", DueDate = DateTime.Now.AddHours(12), Status = "미제출" },
                new Assignment { Id = 2, Course = "SQLite DB", Title = "게시물 권한", DueDate = DateTime.Now.AddDays(3), Status = "제출 완료" },
                new Assignment { Id = 3, Course = "Theme.cs", Title = "버튼 스타일링", DueDate = DateTime.Now.AddDays(10), Status = "미제출" }
            };

            DateTime now = DateTime.Now;
            var dueTodayCount = assignments.Count(a => (a.DueDate - now).TotalHours <= 24 && (a.DueDate - now).TotalHours > 0 && a.Status != "제출 완료");

            DateTime startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
            DateTime endOfWeek = startOfWeek.AddDays(7);

            var dueThisWeek = assignments.Where(a => a.DueDate >= startOfWeek && a.DueDate < endOfWeek).ToList();
            var submittedThisWeekCount = dueThisWeek.Count(a => a.Status == "제출 완료");

            var githubUser = SettingsService.Current?.Integrations.GitHubUser;

            UpdateCardText("card_due_today", $"총 {dueTodayCount}건 (24시간 이내)");
            UpdateCardText("card_due_week", $"총 {dueThisWeek.Count}건 (제출 {submittedThisWeekCount})");

            // GitHub 변경사항 개수 표시
            int changedFiles = _githubService.GetChangedFilesCount();
            
            if (!string.IsNullOrWhiteSpace(githubUser) && !string.IsNullOrWhiteSpace(SettingsService.Current.Integrations.GitHubToken))
            {
                if (changedFiles > 0)
                {
                    UpdateCardText("card_github", $"🔔 변경된 파일: {changedFiles}개\n클릭하여 업로드");
                }
                else
                {
                    UpdateCardText("card_github", $"✅ GitHub 연동됨\n변경사항 없음");
                }
            }
            else
            {
                UpdateCardText("card_github", "통합 설정 필요\n클릭하여 설정으로 이동");
            }
        }

        private void UpdateCardText(string cardTag, string newSubtitle)
        {
            var card = wrap.Controls.OfType<Panel>().FirstOrDefault(p => (p.Tag as string) == cardTag);

            if (card != null)
            {
                var lblSub = card.Controls.OfType<Label>().FirstOrDefault(l => (l.Tag as string) == "subtitle");
                if (lblSub != null)
                {
                    lblSub.Text = newSubtitle;
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
                    var githubToken = SettingsService.Current?.Integrations.GitHubToken;

                    if (string.IsNullOrWhiteSpace(githubToken))
                    {
                        mainForm.NavigateTo<PageSettings>("Integrations");
                    }
                    else
                    {
                        string notificationsUrl = "https://github.com/notifications";
                        try
                        {
                            System.Diagnostics.Process.Start(new ProcessStartInfo(notificationsUrl) { UseShellExecute = true });
                            
                            var refreshTimer = new System.Windows.Forms.Timer();
                            refreshTimer.Interval = 5000;
                            refreshTimer.Tick += (s, args) =>
                            {
                                LoadData();
                                refreshTimer.Stop();
                                refreshTimer.Dispose();
                            };
                            refreshTimer.Start();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"웹 브라우저를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    break;
            }
        }
    }
}