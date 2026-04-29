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

            NotificationService.NotificationCreated += OnNotificationCreated;
            NotificationService.NotificationMarkedAsRead += OnNotificationMarkedAsRead;
            NotificationService.NotificationsAllMarkedAsRead += OnNotificationsAllMarkedAsRead;
            NotificationService.NotificationDeleted += OnNotificationDeleted;

            Theme.ThemeChanged += () => Theme.Apply(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { NotificationService.NotificationCreated -= OnNotificationCreated; } catch { }
                try { NotificationService.NotificationMarkedAsRead -= OnNotificationMarkedAsRead; } catch { }
                try { NotificationService.NotificationsAllMarkedAsRead -= OnNotificationsAllMarkedAsRead; } catch { }
                try { NotificationService.NotificationDeleted -= OnNotificationDeleted; } catch { }
            }
            base.Dispose(disposing);
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
                Text = "알림",
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

            pnlNotifications.Resize += (s, e) =>
            {
                foreach (Control c in pnlNotifications.Controls)
                {
                    if (c is Panel card)
                        card.Width = Math.Max(600, pnlNotifications.ClientSize.Width - 40);
                }
            };

            Controls.Add(pnlNotifications);
            Controls.Add(pnlHeader);
        }

        private void LoadNotifications()
        {
            pnlNotifications.Controls.Clear();

            var notifications = _notificationService.GetNotifications(AuthService.CurrentUser.Id, unreadOnly: true);

            if (notifications.Count == 0)
            {
                pnlNotifications.Controls.Add(MakeEmptyLabel());
                return;
            }

            foreach (var notification in notifications)
                pnlNotifications.Controls.Add(CreateNotificationCard(notification));
        }

        private Label MakeEmptyLabel()
        {
            return new Label
            {
                Text = "알림이 없습니다.",
                Font = new Font("맑은 고딕", 12f),
                ForeColor = Theme.FgMuted,
                AutoSize = true,
                Padding = new Padding(20)
            };
        }

        private Panel CreateNotificationCard(Notification notification)
        {
            var cardWidth = Math.Max(600, pnlNotifications.ClientSize.Width - 40);

            var card = new Panel
            {
                Width = cardWidth,
                Height = 110,
                BackColor = notification.IsRead ? Theme.BgCard : Color.FromArgb(
                    Theme.Current == AppTheme.Dark ? 30 : 240,
                    Theme.Current == AppTheme.Dark ? 50 : 248,
                    Theme.Current == AppTheme.Dark ? 80 : 255),
                Margin = new Padding(0, 0, 0, 12),
                Padding = new Padding(16),
                Cursor = Cursors.Hand,
                Tag = notification
            };
            Theme.StyleCard(card);

            var lblTitle = new Label
            {
                Text = notification.Title,
                Font = new Font("맑은 고딕", 11f, FontStyle.Bold),
                ForeColor = Theme.FgDefault,
                AutoSize = false,
                Location = new Point(10, 8),
                Size = new Size(card.Width - 140, 22)
            };

            var lblMessage = new Label
            {
                Text = notification.Message,
                Font = new Font("맑은 고딕", 10f),
                ForeColor = Theme.FgMuted,
                AutoSize = false,
                Location = new Point(10, 34),
                Size = new Size(card.Width - 140, 36)
            };

            var lblSender = new Label
            {
                Text = $"유형: {(!string.IsNullOrWhiteSpace(notification.Type) ? notification.Type : "시스템")}",
                Font = new Font("맑은 고딕", 9f, FontStyle.Italic),
                ForeColor = Theme.FgMuted,
                AutoSize = true,
                Location = new Point(10, 74)
            };

            var lblTime = new Label
            {
                Text = notification.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
                Font = new Font("맑은 고딕", 9f),
                ForeColor = Theme.FgMuted,
                AutoSize = true,
                Location = new Point(card.Width - 120, 76),
                TextAlign = ContentAlignment.TopRight
            };

            card.Controls.Add(lblTitle);
            card.Controls.Add(lblMessage);
            card.Controls.Add(lblSender);
            card.Controls.Add(lblTime);

            EventHandler onCardClicked = (s, e) =>
            {
                try
                {
                    if (!notification.IsRead)
                    {
                        if (_notificationService.MarkAsRead(notification.Id))
                        {
                            if (pnlNotifications.Controls.Contains(card))
                                pnlNotifications.Controls.Remove(card);

                            if (pnlNotifications.Controls.Count == 0)
                                pnlNotifications.Controls.Add(MakeEmptyLabel());

                            (FindForm() as MainForm)?.UpdateNotificationBadge();
                        }
                    }

                    if (notification.RelatedId.HasValue)
                        NavigateToRelated(notification.Type, notification.RelatedId.Value);
                }
                catch { }
            };

            card.Click += onCardClicked;
            foreach (Control child in card.Controls)
                child.Click += onCardClicked;

            card.Resize += (s, e) =>
            {
                lblTitle.Size = new Size(card.Width - 140, lblTitle.Height);
                lblMessage.Size = new Size(card.Width - 140, lblMessage.Height);
                lblTime.Location = new Point(card.Width - 120, lblTime.Location.Y);
            };

            return card;
        }

        private void NavigateToRelated(string type, int relatedId)
        {
            var mainForm = FindForm() as MainForm;
            if (mainForm == null) return;

            switch (type)
            {
                case "Task":
                case "Team":
                    mainForm.NavigateTo<PageProjectBoard>();
                    break;
                case "Assignment":
                    mainForm.NavigateTo<PageAssignmentDetail>(relatedId);
                    break;
                case "Post":
                case "Board":
                    mainForm.NavigateTo<PagePostDetail>(relatedId);
                    break;
            }
        }

        private void BtnMarkAllRead_Click(object sender, EventArgs e)
        {
            _notificationService.MarkAllAsRead(AuthService.CurrentUser.Id);
            LoadNotifications();
            MessageBox.Show("모든 알림을 읽음 처리했습니다.", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            (FindForm() as MainForm)?.UpdateNotificationBadge();
        }

        private void OnNotificationCreated(Notification n)
        {
            if (AuthService.CurrentUser == null || n.UserId != AuthService.CurrentUser.Id) return;

            if (InvokeRequired) { BeginInvoke(new Action(() => OnNotificationCreated(n))); return; }

            var empty = pnlNotifications.Controls.OfType<Label>().FirstOrDefault(l => l.Text == "알림이 없습니다.");
            if (empty != null) pnlNotifications.Controls.Remove(empty);

            var card = CreateNotificationCard(n);
            pnlNotifications.Controls.Add(card);
            pnlNotifications.Controls.SetChildIndex(card, 0);

            (FindForm() as MainForm)?.UpdateNotificationBadge();
        }

        private void OnNotificationMarkedAsRead(int notificationId)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => OnNotificationMarkedAsRead(notificationId))); return; }

            var card = pnlNotifications.Controls.OfType<Panel>().FirstOrDefault(p =>
            {
                var t = p.Tag as Notification;
                return t != null && t.Id == notificationId;
            });

            if (card != null)
                pnlNotifications.Controls.Remove(card);

            if (pnlNotifications.Controls.Count == 0)
                pnlNotifications.Controls.Add(MakeEmptyLabel());

            (FindForm() as MainForm)?.UpdateNotificationBadge();
        }

        private void OnNotificationsAllMarkedAsRead(int userId)
        {
            if (AuthService.CurrentUser == null || userId != AuthService.CurrentUser.Id) return;
            if (InvokeRequired) { BeginInvoke(new Action(() => OnNotificationsAllMarkedAsRead(userId))); return; }

            pnlNotifications.Controls.Clear();
            pnlNotifications.Controls.Add(MakeEmptyLabel());
            (FindForm() as MainForm)?.UpdateNotificationBadge();
        }

        private void OnNotificationDeleted(int notificationId)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => OnNotificationDeleted(notificationId))); return; }

            var card = pnlNotifications.Controls.OfType<Panel>().FirstOrDefault(p =>
            {
                var t = p.Tag as Notification;
                return t != null && t.Id == notificationId;
            });

            if (card != null)
                pnlNotifications.Controls.Remove(card);

            if (pnlNotifications.Controls.Count == 0)
                pnlNotifications.Controls.Add(MakeEmptyLabel());

            (FindForm() as MainForm)?.UpdateNotificationBadge();
        }
    }
}
