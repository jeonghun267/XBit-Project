// Pages/PageHome.cs (최종 수정본 - 카드 겹침 현상 해결)

using System.Drawing;
using System.Windows.Forms;
using XBit;
using XBit.Services;
using System;
using System.Linq;
using XBit.Models;
using System.Collections.Generic;

namespace XBit.Pages
{
    public class PageHome : UserControl
    {
        private readonly AssignmentService _assignmentService = new AssignmentService();
        private FlowLayoutPanel wrap;

        public PageHome()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgMain;

            wrap = new FlowLayoutPanel
            {
                Width = 900, // 카드가 들어갈 수 있는 적절한 너비 설정
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(8),
                AutoScroll = true
            };
            Theme.EnableDoubleBuffer(wrap);

            // 1. 카드 초기 생성 및 배치
            wrap.Controls.Add(MakeCard("오늘 마감 임박", "데이터 로드 중...", "card_due_today"));
            wrap.Controls.Add(MakeCard("이번 주 과제", "데이터 로드 중...", "card_due_week"));
            wrap.Controls.Add(MakeCard("깃허브 알림", "데이터 로드 중...", "card_github"));

            Controls.Add(wrap);

            LoadData();

            // 2. 테마 변경 시 카드 스타일 업데이트
            Theme.ThemeChanged += () =>
            {
                BackColor = Theme.BgMain;
                foreach (Control c in wrap.Controls)
                {
                    if (c is Panel p) p.BackColor = Theme.BgCard;
                    foreach (Control cc in c.Controls)
                        if (cc is Label l)
                            l.ForeColor = (l.Tag as string) == "muted" ? Theme.FgMuted : Theme.FgDefault;
                }
                Invalidate(true);
            };
        }

        private Panel MakeCard(string title, string subtitle, string tag)
        {
            var card = new Panel
            {
                Width = 280,
                Height = 160,
                Margin = new Padding(8),
                Tag = tag, // 카드 식별자
                BackColor = Theme.BgCard,
                Cursor = Cursors.Hand,
                // ⭐️ 카드 경계선 추가 및 테두리 색상 설정
                BorderStyle = BorderStyle.FixedSingle,
                ForeColor = Theme.Border
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Location = new Point(10, 10), // ⭐️ Location 기반 배치
                AutoSize = true
            };
            Theme.StyleTitle(lblTitle);
            lblTitle.Height = 28;

            var lblSub = new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                Location = new Point(10, 45), // ⭐️ Location 기반 배치 (제목 아래)
                AutoSize = true,
                Tag = "subtitle"
            };
            Theme.StyleMuted(lblSub);

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

        private void LoadData()
        {
            // ⚠️ _assignmentService.GetAssignmentsForUser() 메서드가 AssignmentService에 정의되어 있다고 가정합니다.
            // 임시 데이터 (실제 서비스 호출로 대체하세요)
            List<Assignment> assignments = new List<Assignment>()
            {
                new Assignment { Id = 1, Course = "C# WinForms", Title = "UI 구현", DueDate = DateTime.Now.AddHours(12), Status = "미제출" },
                new Assignment { Id = 2, Course = "SQLite DB", Title = "게시물 권한", DueDate = DateTime.Now.AddDays(3), Status = "제출 완료" },
                new Assignment { Id = 3, Course = "Theme.cs", Title = "버튼 스타일링", DueDate = DateTime.Now.AddDays(10), Status = "미제출" }
            };


            // 1. 오늘 마감 임박 과제 계산 (24시간 이내)
            DateTime now = DateTime.Now;
            var dueTodayCount = assignments.Count(a => (a.DueDate - now).TotalHours <= 24 && (a.DueDate - now).TotalHours > 0 && a.Status != "제출 완료");

            // 2. 이번 주 과제 계산
            DateTime startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
            DateTime endOfWeek = startOfWeek.AddDays(7);

            var dueThisWeek = assignments.Where(a => a.DueDate >= startOfWeek && a.DueDate < endOfWeek).ToList();
            var submittedThisWeekCount = dueThisWeek.Count(a => a.Status == "제출 완료");

            // 3. GitHub 통합 상태 확인
            var githubUser = SettingsService.Current?.Integrations.GitHubUser;

            // ⭐️ UI 업데이트
            UpdateCardText("card_due_today", $"총 {dueTodayCount}건 (24시간 이내)");
            UpdateCardText("card_due_week", $"총 {dueThisWeek.Count}건 (제출 {submittedThisWeekCount})");

            if (!string.IsNullOrWhiteSpace(githubUser) && !string.IsNullOrWhiteSpace(SettingsService.Current.Integrations.GitHubToken))
            {
                UpdateCardText("card_github", $"GitHub 사용자: {githubUser}");
            }
            else
            {
                UpdateCardText("card_github", "통합 설정 필요. 클릭하여 이동");
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
                        MessageBox.Show("GitHub 알림 목록 화면으로 이동 예정.");
                    }
                    break;
            }
        }
    }
}