using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using XBit.Models;
using XBit.Services;

namespace XBit.Pages
{
    public class PageStatistics : UserControl
    {
        private readonly StatisticsService _statisticsService = new StatisticsService();
        private FlowLayoutPanel _flow;
        private Panel _overlay;

        public PageStatistics()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgMain;
            BuildLayout();

            _overlay = Theme.CreateLoadingOverlay("데이터를 불러오는 중입니다...");
            Controls.Add(_overlay);
            _overlay.BringToFront();

            LoadAsync();

            Theme.ThemeChanged += () =>
            {
                BackColor = Theme.BgMain;
                Theme.Apply(this);
                LoadAsync();
            };
        }

        private void BuildLayout()
        {
            // ── Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Theme.BgMain };
            var lblTitle = new Label
            {
                Text = "통계",
                Font = new Font("맑은 고딕", 16f, FontStyle.Bold),
                ForeColor = Theme.FgDefault,
                AutoSize = true,
                Location = new Point(20, 16)
            };
            var btnRefresh = new Button { Text = "새로고침", Width = 90, Height = 32 };
            Theme.StyleButton(btnRefresh);
            btnRefresh.Click += (s, e) => LoadAsync();
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(btnRefresh);
            pnlHeader.Resize += (s, e) => btnRefresh.Location = new Point(pnlHeader.Width - 110, 16);

            var divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.Border };

            // ── Scrollable content (FlowLayoutPanel, top-down)
            _flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(20, 16, 20, 20)
            };
            Theme.EnableDoubleBuffer(_flow);

            Controls.Add(_flow);
            Controls.Add(divider);
            Controls.Add(pnlHeader);
        }

        private async void LoadAsync()
        {
            if (_overlay != null) _overlay.Visible = true;
            _flow.SuspendLayout();
            _flow.Controls.Clear();

            try
            {
                var stats = await Task.Run(() =>
                    _statisticsService.GetUserStatistics(AuthService.CurrentUser.Id));
                RenderStats(stats);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"통계 로드 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _flow.ResumeLayout();
                if (_overlay != null) _overlay.Visible = false;
            }
        }

        private void RenderStats(StatisticsData stats)
        {
            // ── Summary bar
            _flow.Controls.Add(BuildSummaryCard(stats));

            // ── 4 Stat cards (horizontal row wrapped in a panel)
            var rowCards = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 12, 0, 0)
            };
            rowCards.Controls.Add(Theme.CreateStatCard("전체 과제", stats.TotalAssignments.ToString(), Theme.Primary));
            rowCards.Controls.Add(Theme.CreateStatCard("제출 완료", stats.CompletedAssignments.ToString(), Theme.Success));
            rowCards.Controls.Add(Theme.CreateStatCard("읽지 않은 알림", stats.UnreadNotifications.ToString(), Theme.Warning));
            rowCards.Controls.Add(Theme.CreateStatCard("진행중 작업", stats.InProgressTasks.ToString(), Theme.Info));
            _flow.Controls.Add(rowCards);

            // ── Charts row
            var rowCharts = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 12, 0, 0)
            };

            // Donut chart card
            int total = Math.Max(1, stats.TotalAssignments);
            var donutCard = MakeSectionCard("제출 완료율", 200, 200);
            var donut = Theme.CreateDonutChart(stats.CompletedAssignments, total, Theme.Success, "완료율", 120);
            donut.Location = new Point(16, 36);
            donutCard.Controls.Add(donut);

            // Trend chart card
            var trendCard = MakeSectionCard("최근 6개월 활동", 420, 200);
            var trend = Theme.CreateTrendLine(
                stats.MonthlyActivity ?? new int[] { 0, 0, 0, 0, 0, 0 },
                Theme.Primary, 380, 140);
            trend.Location = new Point(8, 36);
            trendCard.Controls.Add(trend);

            rowCharts.Controls.Add(donutCard);
            rowCharts.Controls.Add(trendCard);
            _flow.Controls.Add(rowCharts);

            // Responsive widths
            _flow.Resize += (s, e) =>
            {
                int w = _flow.ClientSize.Width - 40;
                rowCards.Width = w;
                rowCharts.Width = w;
            };
        }

        private Panel BuildSummaryCard(StatisticsData stats)
        {
            int completed = stats.CompletedAssignments;
            int total = stats.TotalAssignments;
            int pct = total == 0 ? 0 : (int)Math.Round(completed * 100.0 / total);
            int overdue = stats.OverdueAssignments;

            var card = new Panel
            {
                Height = 82,
                BackColor = Theme.BgCard,
                Margin = new Padding(0)
            };
            Theme.StyleCard(card);

            var lblSummary = new Label
            {
                Text = $"총 {total}개 과제  ·  완료 {completed}개 ({pct}%)  ·  기한만료 {overdue}개  ·  알림 {stats.UnreadNotifications}개",
                Font = new Font("맑은 고딕", 10f),
                ForeColor = Theme.FgDefault,
                AutoSize = true,
                Location = new Point(0, 0)
            };

            var bar = new ProgressBar
            {
                Value = pct,
                Location = new Point(0, 30),
                Height = 8,
                Style = ProgressBarStyle.Continuous
            };

            card.Controls.Add(lblSummary);
            card.Controls.Add(bar);
            card.Resize += (s, e) => bar.Width = card.Width - 32;

            _flow.Resize += (s, e) => card.Width = _flow.ClientSize.Width - 40;

            return card;
        }

        private Panel MakeSectionCard(string title, int width, int height)
        {
            var card = new Panel
            {
                Width = width,
                Height = height,
                BackColor = Theme.BgCard,
                Margin = new Padding(0, 0, 12, 0)
            };
            Theme.StyleCard(card);

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
                ForeColor = Theme.FgDefault,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            card.Controls.Add(lblTitle);
            return card;
        }
    }
}
