using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using XBit.Models;
using XBit.Services;

namespace XBit.Pages
{
    public class PageStatistics : UserControl
    {
        private readonly StatisticsService _statisticsService = new StatisticsService();
        private FlowLayoutPanel wrap;
        private Panel overlay;

        public PageStatistics()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgMain;
            InitializeLayout();

            overlay = Theme.CreateLoadingOverlay("데이터를 불러오는 중입니다...");
            Controls.Add(overlay);
            overlay.BringToFront();

            LoadStatisticsAsync();
        }

        private void InitializeLayout()
        {
            wrap = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(20),
                AutoScroll = true
            };
            Theme.EnableDoubleBuffer(wrap);
            Controls.Add(wrap);
        }

        private async void LoadStatisticsAsync()
        {
            overlay.Visible = true;
            try
            {
                var stats = await Task.Run(() => _statisticsService.GetUserStatistics(AuthService.CurrentUser.Id));

                wrap.Controls.Clear();

                wrap.Controls.Add(Theme.CreateStatCard("전체 과제", stats.TotalAssignments.ToString(), Theme.Primary));
                wrap.Controls.Add(Theme.CreateStatCard("완료 과제", stats.CompletedAssignments.ToString(), Theme.Success));
                wrap.Controls.Add(Theme.CreateStatCard("읽지 않은 알림", stats.UnreadNotifications.ToString(), Theme.Warning));
                wrap.Controls.Add(Theme.CreateStatCard("진행중 작업", stats.InProgressTasks.ToString(), Theme.Info));

                int total = Math.Max(1, stats.TotalAssignments);
                var donut = Theme.CreateDonutChart(stats.CompletedAssignments, total, Theme.Success, "과제 완료율", 120);
                wrap.Controls.Add(donut);

                var trend = Theme.CreateTrendLine(stats.MonthlyActivity ?? new int[] { 0, 0, 0, 0, 0, 0 }, Theme.Primary, 400, 120);
                var trendWrap = new Panel { Width = 420, Height = 160, BackColor = Theme.BgCard, Margin = new Padding(10) };
                Theme.StyleCard(trendWrap);
                var lbl = new Label { Text = "최근 6개월 활동", Font = new Font("맑은 고딕", 10f, FontStyle.Bold), ForeColor = Theme.FgDefault, Dock = DockStyle.Top, Height = 24 };
                trendWrap.Controls.Add(lbl);
                trendWrap.Controls.Add(trend);
                wrap.Controls.Add(trendWrap);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 로드 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                overlay.Visible = false;
            }
        }
    }
}