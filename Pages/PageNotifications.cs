// XBit/Pages/PageNotifications.cs

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using XBit.Models;
using XBit.Services;

namespace XBit.Pages
{
    public class PageNotifications : UserControl
    {
        private NotificationService _notificationService = new NotificationService();
        private FlowLayoutPanel pnlNotifications;

        public PageNotifications()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgMain;

            InitializeUI();
            LoadNotifications();
        }

        private void InitializeUI()
        {
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Theme.BgMain,
                Padding = new Padding(15)
            };

            var lblTitle = new Label
            {
                Text = "[알림]",
                Font = new Font("맑은 고딕", 16f, FontStyle.Bold),
                ForeColor = Theme.FgDefault,
                AutoSize = true,
                Location = new Point(15, 15)
            };

            var btnMarkAllRead = new Button
            {
                Text = "모두 읽음",
                Width = 100,
                Height = 35,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Theme.StyleButton(btnMarkAllRead);
            btnMarkAllRead.Click += BtnMarkAllRead_Click;

            pnlHeader.Resize += (s, e) =>
            {
                btnMarkAllRead.Location = new Point(pnlHeader.Width - 120, 12);
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(btnMarkAllRead);

            pnlNotifications = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = true,
                Padding = new Padding(15),
                WrapContents = false
            };

            Controls.Add(pnlNotifications);
            Controls.Add(pnlHeader);
        }

        private void LoadNotifications()
        {
            pnlNotifications.Controls.Clear();

            var notifications = _notificationService.GetNotifications(AuthService.CurrentUser.Id);

            if (notifications.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Text = "알림이 없습니다.",
                    Font = new Font("맑은 고딕", 12f),
                    ForeColor = Theme.FgMuted,
                    AutoSize = true,
                    Padding = new Padding(20)
                };
                pnlNotifications.Controls.Add(lblEmpty);
                return;
            }

            foreach (var notification in notifications)
            {
                pnlNotifications.Controls.Add(CreateNotificationCard(notification));
            }
        }

        private Panel CreateNotificationCard(Notification notification)
        {
            var card = new Panel
            {
                Width = pnlNotifications.Width - 50,
                Height = 80,
                BackColor = notification.IsRead ? Theme.BgCard : Color.FromArgb(230, 240, 255),
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(15),
                Cursor = Cursors.Hand
            };
            Theme.StyleCard(card);

            var lblTitle = new Label
            {
                Text = notification.Title,
                Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
                ForeColor = Theme.FgDefault,
                AutoSize = true,
                Location = new Point(10, 10)
            };

            var lblMessage = new Label
            {
                Text = notification.Message,
                Font = new Font("맑은 고딕", 9f),
                ForeColor = Theme.FgMuted,
                AutoSize = true,
                Location = new Point(10, 35),
                MaximumSize = new Size(card.Width - 30, 0)
            };

            var lblTime = new Label
            {
                Text = GetTimeAgo(notification.CreatedDate),
                Font = new Font("맑은 고딕", 8f),
                ForeColor = Theme.FgMuted,
                AutoSize = true,
                Location = new Point(10, 60)
            };

            card.Controls.Add(lblTitle);
            card.Controls.Add(lblMessage);
            card.Controls.Add(lblTime);

            card.Click += (s, e) =>
            {
                if (!notification.IsRead)
                {
                    _notificationService.MarkAsRead(notification.Id);
                    LoadNotifications();
                }

                // 관련 페이지로 이동
                if (notification.RelatedId.HasValue)
                {
                    NavigateToRelated(notification.Type, notification.RelatedId.Value);
                }
            };

            return card;
        }

        private string GetTimeAgo(DateTime date)
        {
            var span = DateTime.Now - date;

            if (span.TotalMinutes < 1)
                return "방금 전";
            if (span.TotalMinutes < 60)
                return $"{(int)span.TotalMinutes}분 전";
            if (span.TotalHours < 24)
                return $"{(int)span.TotalHours}시간 전";
            if (span.TotalDays < 7)
                return $"{(int)span.TotalDays}일 전";

            return date.ToString("yyyy-MM-dd");
        }

        private void NavigateToRelated(string type, int relatedId)
        {
            var mainForm = FindForm() as MainForm;
            if (mainForm == null) return;

            switch (type)
            {
                case "Task":
                    mainForm.NavigateTo<PageProjectBoard>();
                    break;
                case "Assignment":
                    mainForm.NavigateTo<PageAssignmentDetail>(relatedId);
                    break;
                case "Team":
                    mainForm.NavigateTo<PageProjectBoard>();
                    break;
            }
        }

        private void BtnMarkAllRead_Click(object sender, EventArgs e)
        {
            _notificationService.MarkAllAsRead(AuthService.CurrentUser.Id);
            LoadNotifications();
            MessageBox.Show("모든 알림을 읽음 처리했습니다.", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}