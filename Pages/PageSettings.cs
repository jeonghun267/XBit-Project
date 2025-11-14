using System;
using System.Drawing;
using System.Windows.Forms;
using XBit.Models;
using XBit.Services;
using XBit;

namespace XBit.Pages
{
    public class PageSettings : UserControl
    {
        public PageSettings()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgMain;
            AutoScroll = true;
            Padding = new Padding(12);

            // Controls는 역순으로 추가해야 DockStyle.Top으로 설정된 컨트롤이 순서대로 배치됨
            Controls.Add(MakeSection_DangerZone());
            Controls.Add(MakeSection_Integrations());
            Controls.Add(MakeSection_Security());
            Controls.Add(MakeSection_Privacy());
            Controls.Add(MakeSection_Notifications());
            Controls.Add(MakeSection_Appearance());
            Controls.Add(MakeSection_Account());
            Controls.Add(MakeSection_Profile());

            Theme.Apply(this);
            Theme.ThemeChanged += () => Theme.Apply(this);
        }

        /// <summary>
        /// 레이아웃 짤림 문제를 해결하고 카드 내부 여백을 확보하도록 수정
        /// </summary>
        private Panel MakeSection(string titleText, Control content)
        {
            // 카드 내부 여백 확보: Padding을 늘려서 섹션 제목과 내용의 시각적 안정성을 높임
            var title = new Label { Text = titleText };
            Theme.StyleTitle(title);
            title.Dock = DockStyle.Top;

            content.Margin = new Padding(0, 5, 0, 0); // 제목과의 간격
            content.Dock = DockStyle.Top;

            // 제목과 내용을 모두 담을 FlowLayoutPanel 래퍼를 생성
            var flowWrapper = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(20, 15, 20, 20) // ⭐️ 여백 증가 (좌우 20, 상 15, 하 20)
            };
            flowWrapper.Controls.Add(title);
            flowWrapper.Controls.Add(content);

            var card = new Panel
            {
                Tag = "card",
                Margin = new Padding(0, 0, 0, 20), // ⭐️ 섹션 간 간격 증가 (12 -> 20)
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            Theme.StyleCard(card);

            card.Controls.Add(flowWrapper);

            return card;
        }

        private Panel MakeSection_Profile()
        {
            var p = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 2,
                Dock = DockStyle.Top,
                AutoSize = true
            };
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var user = AuthService.CurrentUser;
            var s = SettingsService.Current;

            p.Controls.Add(new Label { Text = "이름", AutoSize = true, Tag = "muted", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 0);
            var txtName = new TextBox { Text = user?.Name, Dock = DockStyle.Fill };
            p.Controls.Add(txtName, 1, 0);

            p.Controls.Add(new Label { Text = "이메일", AutoSize = true, Tag = "muted", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 1);
            var txtEmail = new TextBox { Text = user?.Email, Dock = DockStyle.Fill };
            p.Controls.Add(txtEmail, 1, 1);

            var card = MakeSection("프로필", p);

            void Save()
            {
                if (user == null) return;

                user.Name = txtName.Text.Trim();
                user.Email = txtEmail.Text.Trim();

                AuthService.UpdateUser(user);
            }

            txtName.Leave += (_, __) => Save();
            txtEmail.Leave += (_, __) => Save();

            return card;
        }

        private Panel MakeSection_Account()
        {
            var p = new TableLayoutPanel { ColumnCount = 2, RowCount = 2, Dock = DockStyle.Top, AutoSize = true };
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var user = AuthService.CurrentUser;
            var s = SettingsService.Current;

            p.Controls.Add(new Label { Text = "유저명", AutoSize = true, Tag = "muted", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 0);
            var txtUser = new TextBox { Text = user?.Username, Dock = DockStyle.Fill, ReadOnly = true };
            p.Controls.Add(txtUser, 1, 0);

            p.Controls.Add(new Label { Text = "프로바이더", AutoSize = true, Tag = "muted", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 1);
            var txtProv = new TextBox { Text = s.Account.Provider, Dock = DockStyle.Fill };
            p.Controls.Add(txtProv, 1, 1);

            var card = MakeSection("계정", p);

            void Save()
            {
                s.Account.Provider = txtProv.Text.Trim();
                SettingsService.Save();
            }

            txtProv.Leave += (_, __) => Save();

            return card;
        }

        private Panel MakeSection_Appearance()
        {
            var wrap = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight // ⭐️ 수평 배치로 변경
            };

            var chkDark = new CheckBox
            {
                Text = "다크 모드 사용",
                Checked = (Theme.Current == AppTheme.Dark),
                AutoSize = true,
                Margin = new Padding(0, 0, 20, 0) // ⭐️ 체크박스 간 수평 간격 추가
            };
            chkDark.CheckedChanged += (_, __) =>
            {
                Theme.Set(chkDark.Checked ? AppTheme.Dark : AppTheme.Light);
                SettingsService.SetTheme(Theme.Current);
            };

            wrap.Controls.Add(chkDark);
            return MakeSection("모양", wrap);
        }

        private Panel MakeSection_Notifications()
        {
            var s = SettingsService.Current;
            var wrap = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight, // ⭐️ 수평 배치로 변경
                WrapContents = true // 줄 바꿈 허용
            };

            var c1 = new CheckBox { Text = "앱 업데이트 알림", Checked = s.Notifications.AppUpdates, AutoSize = true, Margin = new Padding(0, 0, 20, 0) };
            var c2 = new CheckBox { Text = "과제 마감 알림", Checked = s.Notifications.AssignmentDue, AutoSize = true, Margin = new Padding(0, 0, 20, 0) };
            var c3 = new CheckBox { Text = "PR 알림", Checked = s.Notifications.PullRequests, AutoSize = true, Margin = new Padding(0, 0, 20, 0) };
            var c4 = new CheckBox { Text = "이슈 알림", Checked = s.Notifications.Issues, AutoSize = true, Margin = new Padding(0, 0, 20, 0) };

            void Save()
            {
                s.Notifications.AppUpdates = c1.Checked;
                s.Notifications.AssignmentDue = c2.Checked;
                s.Notifications.PullRequests = c3.Checked;
                s.Notifications.Issues = c4.Checked;
                SettingsService.Save();
            }

            c1.CheckedChanged += (_, __) => Save();
            c2.CheckedChanged += (_, __) => Save();
            c3.CheckedChanged += (_, __) => Save();
            c4.CheckedChanged += (_, __) => Save();

            wrap.Controls.AddRange(new Control[] { c1, c2, c3, c4 });
            return MakeSection("알림", wrap);
        }

        private Panel MakeSection_Privacy()
        {
            var s = SettingsService.Current;
            var wrap = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight, // ⭐️ 수평 배치로 변경
                WrapContents = true
            };

            var c1 = new CheckBox { Text = "사용 통계 공유", Checked = s.Privacy.ShareUsageStats, AutoSize = true, Margin = new Padding(0, 0, 20, 0) };
            var c2 = new CheckBox { Text = "프로필에 이메일 표시", Checked = s.Privacy.ShowEmailInProfile, AutoSize = true, Margin = new Padding(0, 0, 20, 0) };

            void Save()
            {
                s.Privacy.ShareUsageStats = c1.Checked;
                s.Privacy.ShowEmailInProfile = c2.Checked;
                SettingsService.Save();
            }

            c1.CheckedChanged += (_, __) => Save();
            c2.CheckedChanged += (_, __) => Save();

            wrap.Controls.AddRange(new Control[] { c1, c2 });
            return MakeSection("개인정보", wrap);
        }

        private Panel MakeSection_Security()
        {
            var s = SettingsService.Current;
            var wrap = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight, // ⭐️ 수평 배치로 변경
                WrapContents = true
            };

            var c1 = new CheckBox { Text = "실행 시 비밀번호 요구", Checked = s.Security.RequirePasswordOnStart, AutoSize = true, Margin = new Padding(0, 0, 20, 0) };
            var c2 = new CheckBox { Text = "생체인증 사용", Checked = s.Security.BiometricEnabled, AutoSize = true, Margin = new Padding(0, 0, 20, 0) };

            void Save()
            {
                s.Security.RequirePasswordOnStart = c1.Checked;
                s.Security.BiometricEnabled = c2.Checked;
                SettingsService.Save();
            }

            c1.CheckedChanged += (_, __) => Save();
            c2.CheckedChanged += (_, __) => Save();

            wrap.Controls.AddRange(new Control[] { c1, c2 });
            return MakeSection("보안", wrap);
        }

        private Panel MakeSection_Integrations()
        {
            var s = SettingsService.Current;
            var p = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Top, AutoSize = true };
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            p.Controls.Add(new Label { Text = "GitHub User", AutoSize = true, Tag = "muted" }, 0, 0);
            var txtUser = new TextBox { Text = s.Integrations.GitHubUser, Dock = DockStyle.Fill };
            p.Controls.Add(txtUser, 1, 0);

            p.Controls.Add(new Label { Text = "GitHub Token", AutoSize = true, Tag = "muted" }, 0, 1);
            var txtToken = new TextBox { Text = s.Integrations.GitHubToken, Dock = DockStyle.Fill, UseSystemPasswordChar = true };
            p.Controls.Add(txtToken, 1, 1);

            void Save()
            {
                s.Integrations.GitHubUser = txtUser.Text.Trim();
                s.Integrations.GitHubToken = txtToken.Text.Trim();
                SettingsService.Save();
            }
            txtUser.Leave += (_, __) => Save();
            txtToken.Leave += (_, __) => Save();

            return MakeSection("통합", p);
        }

        private Panel MakeSection_DangerZone()
        {
            var wrap = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight
            };

            var btnReset = new Button { Text = "모든 설정 초기화" };
            Theme.StyleDanger(btnReset);
            btnReset.Click += (_, __) =>
            {
                if (MessageBox.Show("설정을 기본값으로 초기화할까요?",
                                     "초기화",
                                     MessageBoxButtons.YesNo,
                                     MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    SettingsService.Reset();

                    Theme.Apply(this);

                    MessageBox.Show("초기화 완료", "완료");
                }
            };

            var btnDeleteAccount = new Button { Text = "회원 탈퇴", Margin = new Padding(10, 0, 0, 0) };
            Theme.StyleDanger(btnDeleteAccount);
            btnDeleteAccount.Click += (_, __) =>
            {
                if (MessageBox.Show("정말로 계정을 삭제하시겠습니까? 모든 정보가 사라집니다.",
                                     "경고",
                                     MessageBoxButtons.YesNo,
                                     MessageBoxIcon.Stop) == DialogResult.Yes)
                {
                    if (AuthService.CurrentUser != null && AuthService.DeleteUser(AuthService.CurrentUser.Id))
                    {
                        MessageBox.Show("계정이 삭제되었습니다.", "완료");
                        AuthService.Logout();
                        Application.Restart();
                    }
                    else
                    {
                        MessageBox.Show("계정 삭제에 실패했습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };


            wrap.Controls.Add(btnReset);
            wrap.Controls.Add(btnDeleteAccount);

            return MakeSection("Danger Zone", wrap);
        }
    }
}