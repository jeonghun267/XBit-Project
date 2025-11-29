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

            // 이벤트 구독: 새로운 알림 추가, 단일/전체 읽음, 삭제
            NotificationService.NotificationCreated += OnNotificationCreated;
            NotificationService.NotificationMarkedAsRead += OnNotificationMarkedAsRead;
            NotificationService.NotificationsAllMarkedAsRead += OnNotificationsAllMarkedAsRead;
            NotificationService.NotificationDeleted += OnNotificationDeleted;
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

            // 반응형: 컨테이너 크기 변경 시 카드 너비 갱신
            pnlNotifications.Resize += (s, e) =>
            {
                foreach (Control c in pnlNotifications.Controls)
                {
                    if (c is Panel card)
                    {
                        card.Width = Math.Max(600, pnlNotifications.ClientSize.Width - 40);
                        // 재계산이 필요한 내부 라벨들 자동 레이아웃으로 처리
                    }
                }
            };

            Controls.Add(pnlNotifications);
            Controls.Add(pnlHeader);
        }

        private void LoadNotifications()
        {
            pnlNotifications.Controls.Clear();

            // 변경: 읽지 않은 알림만 표시하도록 (읽음 처리 시 목록에서 사라짐)
            var notifications = _notificationService.GetNotifications(AuthService.CurrentUser.Id, unreadOnly: true);

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
            // 더 넓고 읽기 쉬운 카드 스타일
            var cardWidth = Math.Max(600, pnlNotifications.ClientSize.Width - 40);

            var card = new Panel
            {
                Width = cardWidth,
                Height = 110,
                BackColor = notification.IsRead ? Theme.BgCard : Color.FromArgb(240, 248, 255),
                Margin = new Padding(0, 0, 0, 12),
                Padding = new Padding(16),
                Cursor = Cursors.Hand,
                Tag = notification
            };
            Theme.StyleCard(card);

            // 제목
            var lblTitle = new Label
            {
                Text = notification.Title,
                Font = new Font("맑은 고딕", 11f, FontStyle.Bold),
                ForeColor = Theme.FgDefault,
                AutoSize = false,
                Location = new Point(10, 8),
                Size = new Size(card.Width - 140, 22)
            };

            // 메시지 (wrap, multiline)
            var lblMessage = new Label
            {
                Text = notification.Message,
                Font = new Font("맑은 고딕", 10f),
                ForeColor = Theme.FgMuted,
                AutoSize = false,
                Location = new Point(10, 34),
                Size = new Size(card.Width - 140, 36)
            };

            // 보낸이
            var lblSender = new Label
            {
                Text = $"보낸이: {(!string.IsNullOrWhiteSpace(notification.Type) ? notification.Type : "시스템")}",
                Font = new Font("맑은 고딕", 9f, FontStyle.Italic),
                ForeColor = Theme.FgMuted,
                AutoSize = true,
                Location = new Point(10, 74)
            };

            // 정확한 수신 시간 (하단 우측)
            var lblExactTime = new Label
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
            card.Controls.Add(lblExactTime);

            // 클릭 처리 로직
            EventHandler onCardClicked = (s, e) =>
            {
                try
                {
                    if (!notification.IsRead)
                    {
                        var ok = _notificationService.MarkAsRead(notification.Id);
                        if (ok)
                        {
                            // 읽음 처리되면 현재 카드 제거
                            if (pnlNotifications.Controls.Contains(card))
                                pnlNotifications.Controls.Remove(card);

                            // 상단 배지 갱신 호출 (MainForm에 구현된 메서드 사용)
                            var mainForm = FindForm() as MainForm;
                            mainForm?.UpdateNotificationBadge();
                        }
                    }

                    if (notification.RelatedId.HasValue)
                    {
                        NavigateToRelated(notification.Type, notification.RelatedId.Value);
                    }
                }
                catch
                {
                    // 로깅/무시
                }
            };

            // 카드와 내부 컨트롤 모두 동일한 핸들러 연결
            card.Click += onCardClicked;
            foreach (Control child in card.Controls)
            {
                child.Click += onCardClicked;
            }

            // 카드 너비가 변경될 경우 내부 요소 위치 보정
            card.Resize += (s, e) =>
            {
                lblTitle.Size = new Size(card.Width - 140, lblTitle.Height);
                lblMessage.Size = new Size(card.Width - 140, lblMessage.Height);
                lblExactTime.Location = new Point(card.Width - 120, lblExactTime.Location.Y);
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
                    mainForm.NavigateTo<PageProjectBoard>();
                    break;
                case "Assignment":
                    mainForm.NavigateTo<PageAssignmentDetail>(relatedId);
                    break;
                case "Team":
                    mainForm.NavigateTo<PageProjectBoard>();
                    break;
                default:
                    // 시스템 등 기타 타입은 알림 상세 페이지가 없으면 무시
                    break;
            }
        }

        private void BtnMarkAllRead_Click(object sender, EventArgs e)
        {
            _notificationService.MarkAllAsRead(AuthService.CurrentUser.Id);
            LoadNotifications();
            MessageBox.Show("모든 알림을 읽음 처리했습니다.", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // 상단 배지 갱신
            var mainForm = FindForm() as MainForm;
            mainForm?.UpdateNotificationBadge();
        }

        // 이벤트 핸들러: 새 알림이 생성되면(같은 프로세스) 즉시 추가
        private void OnNotificationCreated(Notification n)
        {
            if (AuthService.CurrentUser == null || n.UserId != AuthService.CurrentUser.Id) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnNotificationCreated(n)));
                return;
            }

            // "알림이 없습니다." 라벨 제거
            var empty = pnlNotifications.Controls.OfType<Label>().FirstOrDefault(l => l.Text == "알림이 없습니다.");
            if (empty != null) pnlNotifications.Controls.Remove(empty);

            var card = CreateNotificationCard(n);
            pnlNotifications.Controls.Add(card);
            pnlNotifications.Controls.SetChildIndex(card, 0); // 맨 위에 추가

            // 배지 갱신
            var mainForm = FindForm() as MainForm;
            mainForm?.UpdateNotificationBadge();
        }

        private void OnNotificationMarkedAsRead(int notificationId)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnNotificationMarkedAsRead(notificationId)));
                return;
            }

            // 해당 카드 제거
            var card = pnlNotifications.Controls.OfType<Panel>().FirstOrDefault(p =>
            {
                var t = p.Tag as Notification;
                return t != null && t.Id == notificationId;
            });

            if (card != null)
            {
                pnlNotifications.Controls.Remove(card);
            }

            // 배지 갱신
            var mainForm = FindForm() as MainForm;
            mainForm?.UpdateNotificationBadge();

            // 빈 상태 표시
            if (pnlNotifications.Controls.Count == 0)
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
            }
        }

        private void OnNotificationsAllMarkedAsRead(int userId)
        {
            if (AuthService.CurrentUser == null || userId != AuthService.CurrentUser.Id) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnNotificationsAllMarkedAsRead(userId)));
                return;
            }

            // 모든 카드 제거(읽지 않던 항목만 있던 컨트롤 구조이므로 전체 초기화)
            pnlNotifications.Controls.Clear();

            var lblEmpty = new Label
            {
                Text = "알림이 없습니다.",
                Font = new Font("맑은 고딕", 12f),
                ForeColor = Theme.FgMuted,
                AutoSize = true,
                Padding = new Padding(20)
            };
            pnlNotifications.Controls.Add(lblEmpty);

            var mainForm = FindForm() as MainForm;
            mainForm?.UpdateNotificationBadge();
        }

        private void OnNotificationDeleted(int notificationId)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnNotificationDeleted(notificationId)));
                return;
            }

            var card = pnlNotifications.Controls.OfType<Panel>().FirstOrDefault(p =>
            {
                var t = p.Tag as Notification;
                return t != null && t.Id == notificationId;
            });

            if (card != null) pnlNotifications.Controls.Remove(card);

            var mainForm = FindForm() as MainForm;
            mainForm?.UpdateNotificationBadge();
        }
    }
}