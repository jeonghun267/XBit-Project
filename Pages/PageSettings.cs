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
        private Panel pnlSidebar;
        private Panel pnlContent;
        private Button _activeNavBtn;

        public PageSettings()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgMain;
            Padding = new Padding(0);

            BuildLayout();

            Theme.Apply(this);
            Theme.ThemeChanged += () => Theme.Apply(this);
        }

        // ───────────────────────────────────────────
        // 전체 레이아웃
        // ───────────────────────────────────────────
        private void BuildLayout()
        {
            // 상단 헤더
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Theme.BgSidebar,
                Padding = new Padding(20, 0, 0, 0)
            };
            var lblHeader = new Label
            {
                Text = "설정",
                Font = new Font("맑은 고딕", 15f, FontStyle.Bold),
                ForeColor = Theme.FgDefault,
                AutoSize = true,
                Dock = DockStyle.Left
            };
            lblHeader.TextAlign = ContentAlignment.MiddleLeft;
            pnlHeader.Controls.Add(lblHeader);

            // 본문 분할
            var split = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            split.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // 좌측 사이드바
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgSidebar,
                Padding = new Padding(0, 8, 0, 8)
            };

            // 우측 컨텐츠
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgMain,
                AutoScroll = true,
                Padding = new Padding(24, 20, 24, 20)
            };

            split.Controls.Add(pnlSidebar, 0, 0);
            split.Controls.Add(pnlContent, 1, 0);

            Controls.Add(split);
            Controls.Add(pnlHeader);

            BuildSidebar();

            // 기본으로 첫 번째 섹션(프로필) 표시
            ShowSection(BuildSection_Profile(), GetNavButton("프로필"));
        }

        // ───────────────────────────────────────────
        // 사이드바 네비게이션
        // ───────────────────────────────────────────
        private void BuildSidebar()
        {
            var items = new[]
            {
                ("👤  프로필",      (Func<Control>)BuildSection_Profile),
                ("🔑  계정",        (Func<Control>)BuildSection_Account),
                ("🎨  모양",        (Func<Control>)BuildSection_Appearance),
                ("🔔  알림",        (Func<Control>)BuildSection_Notifications),
                ("🔒  개인정보",    (Func<Control>)BuildSection_Privacy),
                ("🛡  보안",        (Func<Control>)BuildSection_Security),
                ("🔗  GitHub 연동", (Func<Control>)BuildSection_Integrations),
                ("⚠  위험 구역",   (Func<Control>)BuildSection_DangerZone),
            };

            foreach (var (label, builder) in items)
            {
                var captured_label = label;
                var captured_builder = builder;

                var btn = new Button
                {
                    Text = label,
                    Dock = DockStyle.Top,
                    Height = 42,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(16, 0, 8, 0),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("맑은 고딕", 10f),
                    BackColor = Theme.BgSidebar,
                    ForeColor = Theme.FgPrimary,
                    Cursor = Cursors.Hand,
                    Tag = "btn-nav"
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = Theme.Hover;

                btn.Click += (s, e) =>
                {
                    ShowSection(captured_builder(), btn);
                };

                pnlSidebar.Controls.Add(btn);
            }

            // 역순으로 추가되므로 BringToFront로 순서 조정
            foreach (Control c in pnlSidebar.Controls)
                c.BringToFront();
        }

        private Button GetNavButton(string text)
        {
            foreach (Control c in pnlSidebar.Controls)
            {
                if (c is Button b && b.Text.Contains(text.Replace("👤  ", "").Replace("🔑  ", "")))
                    return b;
            }
            return null;
        }

        private void ShowSection(Control section, Button navBtn)
        {
            // 이전 활성 버튼 스타일 복원
            if (_activeNavBtn != null)
            {
                _activeNavBtn.BackColor = Theme.BgSidebar;
                _activeNavBtn.ForeColor = Theme.FgPrimary;
                _activeNavBtn.Font = new Font("맑은 고딕", 10f, FontStyle.Regular);
            }

            // 새 활성 버튼 강조
            if (navBtn != null)
            {
                navBtn.BackColor = Theme.Selected;
                navBtn.ForeColor = Theme.Primary;
                navBtn.Font = new Font("맑은 고딕", 10f, FontStyle.Bold);
                _activeNavBtn = navBtn;
            }

            // 콘텐츠 교체
            pnlContent.Controls.Clear();
            if (section != null)
            {
                section.Dock = DockStyle.Top;
                pnlContent.Controls.Add(section);
                Theme.Apply(section);
            }

            pnlContent.AutoScrollPosition = new Point(0, 0);
        }

        // ───────────────────────────────────────────
        // 공통 카드 래퍼
        // ───────────────────────────────────────────
        private Panel WrapCard(string title, string subtitle, Control content)
        {
            var card = new Panel
            {
                Tag = "card",
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 16),
                Padding = new Padding(20, 16, 20, 20)
            };
            Theme.StyleCard(card);

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("맑은 고딕", 12f, FontStyle.Bold),
                ForeColor = Theme.FgDefault,
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 0, 0, 4)
            };

            var inner = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 8, 0, 0)
            };

            if (!string.IsNullOrEmpty(subtitle))
            {
                var lblSub = new Label
                {
                    Text = subtitle,
                    Font = new Font("맑은 고딕", 9f),
                    ForeColor = Theme.FgMuted,
                    AutoSize = true,
                    Tag = "muted",
                    Dock = DockStyle.Top,
                    Padding = new Padding(0, 0, 0, 10)
                };
                inner.Controls.Add(lblSub);
            }

            content.Dock = DockStyle.Top;
            inner.Controls.Add(content);

            card.Controls.Add(inner);
            card.Controls.Add(lblTitle);

            return card;
        }

        // TableLayoutPanel 2열 폼 헬퍼
        private TableLayoutPanel MakeFormGrid(int rows)
        {
            var grid = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = rows,
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(0)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < rows; i++)
                grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            return grid;
        }

        private Label MakeFieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Tag = "muted",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                Font = new Font("맑은 고딕", 9f)
            };
        }

        // ───────────────────────────────────────────
        // 섹션: 프로필
        // ───────────────────────────────────────────
        private Control BuildSection_Profile()
        {
            var user = AuthService.CurrentUser;
            var grid = MakeFormGrid(2);

            grid.Controls.Add(MakeFieldLabel("이름"), 0, 0);
            var txtName = new TextBox { Text = user?.Name ?? "", Dock = DockStyle.Fill };
            grid.Controls.Add(txtName, 1, 0);

            grid.Controls.Add(MakeFieldLabel("이메일"), 0, 1);
            var txtEmail = new TextBox { Text = user?.Email ?? "", Dock = DockStyle.Fill };
            grid.Controls.Add(txtEmail, 1, 1);

            void Save()
            {
                if (user == null) return;
                user.Name = txtName.Text.Trim();
                user.Email = txtEmail.Text.Trim();
                AuthService.UpdateUser(user);
            }
            txtName.Leave += (_, __) => Save();
            txtEmail.Leave += (_, __) => Save();

            return WrapCard("프로필", "로그인 계정에 표시되는 이름과 이메일을 설정합니다.", grid);
        }

        // ───────────────────────────────────────────
        // 섹션: 계정
        // ───────────────────────────────────────────
        private Control BuildSection_Account()
        {
            var user = AuthService.CurrentUser;
            var s = SettingsService.Current;
            var grid = MakeFormGrid(2);

            grid.Controls.Add(MakeFieldLabel("유저명"), 0, 0);
            var txtUser = new TextBox { Text = user?.Username ?? "", Dock = DockStyle.Fill, ReadOnly = true };
            grid.Controls.Add(txtUser, 1, 0);

            grid.Controls.Add(MakeFieldLabel("프로바이더"), 0, 1);
            var txtProv = new TextBox { Text = s.Account.Provider, Dock = DockStyle.Fill };
            grid.Controls.Add(txtProv, 1, 1);

            txtProv.Leave += (_, __) => { s.Account.Provider = txtProv.Text.Trim(); SettingsService.Save(); };

            return WrapCard("계정", "계정 유형과 로그인 방식입니다.", grid);
        }

        // ───────────────────────────────────────────
        // 섹션: 모양
        // ───────────────────────────────────────────
        private Control BuildSection_Appearance()
        {
            var wrap = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(0, 4, 0, 0)
            };

            // 라이트 / 다크 토글 버튼 스타일
            var btnLight = MakeThemeToggle("☀  라이트 모드", Theme.Current == AppTheme.Light);
            var btnDark  = MakeThemeToggle("🌙  다크 모드",  Theme.Current == AppTheme.Dark);

            btnLight.Click += (_, __) =>
            {
                ApplyTheme(AppTheme.Light, btnLight, btnDark);
            };
            btnDark.Click += (_, __) =>
            {
                ApplyTheme(AppTheme.Dark, btnDark, btnLight);
            };

            wrap.Controls.Add(btnLight);
            wrap.Controls.Add(btnDark);

            return WrapCard("모양", "앱 테마를 선택합니다.", wrap);
        }

        private Button MakeThemeToggle(string text, bool isActive)
        {
            var btn = new Button
            {
                Text = text,
                Width = 160,
                Height = 52,
                Margin = new Padding(0, 0, 12, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 10f, isActive ? FontStyle.Bold : FontStyle.Regular),
                BackColor = isActive ? Theme.Primary : Theme.BgCard,
                ForeColor = isActive ? Color.White : Theme.FgPrimary,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = isActive ? 0 : 1;
            btn.FlatAppearance.BorderColor = Theme.Border;
            return btn;
        }

        private void ApplyTheme(AppTheme target, Button activated, Button deactivated)
        {
            Theme.Set(target);
            SettingsService.SetTheme(Theme.Current);

            activated.BackColor = Theme.Primary;
            activated.ForeColor = Color.White;
            activated.Font = new Font("맑은 고딕", 10f, FontStyle.Bold);
            activated.FlatAppearance.BorderSize = 0;

            deactivated.BackColor = Theme.BgCard;
            deactivated.ForeColor = Theme.FgPrimary;
            deactivated.Font = new Font("맑은 고딕", 10f, FontStyle.Regular);
            deactivated.FlatAppearance.BorderSize = 1;
            deactivated.FlatAppearance.BorderColor = Theme.Border;

            (FindForm() as MainForm)?.let(f => Theme.Apply(f));
        }

        // ───────────────────────────────────────────
        // 섹션: 알림
        // ───────────────────────────────────────────
        private Control BuildSection_Notifications()
        {
            var s = SettingsService.Current;
            var stack = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0)
            };

            var c1 = MakeToggleRow("앱 업데이트 알림",  "새 버전 출시 시 알림을 받습니다.", s.Notifications.AppUpdates);
            var c2 = MakeToggleRow("과제 마감 알림",     "마감 24시간 전 알림을 받습니다.", s.Notifications.AssignmentDue);
            var c3 = MakeToggleRow("PR 알림",            "Pull Request 이벤트 알림입니다.", s.Notifications.PullRequests);
            var c4 = MakeToggleRow("이슈 알림",          "이슈 생성/변경 알림입니다.", s.Notifications.Issues);

            void Save()
            {
                s.Notifications.AppUpdates    = ((CheckBox)c1.Controls[0]).Checked;
                s.Notifications.AssignmentDue = ((CheckBox)c2.Controls[0]).Checked;
                s.Notifications.PullRequests  = ((CheckBox)c3.Controls[0]).Checked;
                s.Notifications.Issues        = ((CheckBox)c4.Controls[0]).Checked;
                SettingsService.Save();
            }

            foreach (var row in new[] { c1, c2, c3, c4 })
            {
                ((CheckBox)row.Controls[0]).CheckedChanged += (_, __) => Save();
                stack.Controls.Add(row);
            }

            return WrapCard("알림", "받을 알림 종류를 선택합니다.", stack);
        }

        // ───────────────────────────────────────────
        // 섹션: 개인정보
        // ───────────────────────────────────────────
        private Control BuildSection_Privacy()
        {
            var s = SettingsService.Current;
            var stack = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            var c1 = MakeToggleRow("사용 통계 공유",       "익명 사용 통계를 전송합니다.", s.Privacy.ShareUsageStats);
            var c2 = MakeToggleRow("프로필에 이메일 표시", "다른 사용자에게 이메일이 보입니다.", s.Privacy.ShowEmailInProfile);

            void Save()
            {
                s.Privacy.ShareUsageStats     = ((CheckBox)c1.Controls[0]).Checked;
                s.Privacy.ShowEmailInProfile  = ((CheckBox)c2.Controls[0]).Checked;
                SettingsService.Save();
            }

            foreach (var row in new[] { c1, c2 })
            {
                ((CheckBox)row.Controls[0]).CheckedChanged += (_, __) => Save();
                stack.Controls.Add(row);
            }

            return WrapCard("개인정보", "공개 범위와 개인정보 관련 설정입니다.", stack);
        }

        // ───────────────────────────────────────────
        // 섹션: 보안
        // ───────────────────────────────────────────
        private Control BuildSection_Security()
        {
            var s = SettingsService.Current;
            var stack = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            var c1 = MakeToggleRow("실행 시 비밀번호 요구", "앱 시작 시 비밀번호를 입력해야 합니다.", s.Security.RequirePasswordOnStart);
            var c2 = MakeToggleRow("생체인증 사용",          "지문/얼굴 인식으로 로그인합니다.", s.Security.BiometricEnabled);

            void Save()
            {
                s.Security.RequirePasswordOnStart = ((CheckBox)c1.Controls[0]).Checked;
                s.Security.BiometricEnabled       = ((CheckBox)c2.Controls[0]).Checked;
                SettingsService.Save();
            }

            foreach (var row in new[] { c1, c2 })
            {
                ((CheckBox)row.Controls[0]).CheckedChanged += (_, __) => Save();
                stack.Controls.Add(row);
            }

            return WrapCard("보안", "계정 보안 옵션을 설정합니다.", stack);
        }

        // ───────────────────────────────────────────
        // 섹션: GitHub 연동
        // ───────────────────────────────────────────
        private Control BuildSection_Integrations()
        {
            var s = SettingsService.Current;
            var grid = MakeFormGrid(2);

            grid.Controls.Add(MakeFieldLabel("GitHub 유저"), 0, 0);
            var txtUser = new TextBox { Text = s.Integrations.GitHubUser, Dock = DockStyle.Fill };
            grid.Controls.Add(txtUser, 1, 0);

            grid.Controls.Add(MakeFieldLabel("액세스 토큰"), 0, 1);
            var txtToken = new TextBox { Text = s.Integrations.GitHubToken, Dock = DockStyle.Fill, UseSystemPasswordChar = true };
            grid.Controls.Add(txtToken, 1, 1);

            void Save()
            {
                s.Integrations.GitHubUser  = txtUser.Text.Trim();
                s.Integrations.GitHubToken = txtToken.Text.Trim();
                SettingsService.Save();
            }
            txtUser.Leave  += (_, __) => Save();
            txtToken.Leave += (_, __) => Save();

            // 토큰 보기/숨기기 버튼
            var btnToggleToken = new Button { Text = "표시", Width = 60, Height = 28, Margin = new Padding(0, 4, 0, 0) };
            Theme.StyleButton(btnToggleToken);
            btnToggleToken.Click += (_, __) =>
            {
                txtToken.UseSystemPasswordChar = !txtToken.UseSystemPasswordChar;
                btnToggleToken.Text = txtToken.UseSystemPasswordChar ? "표시" : "숨기기";
            };

            var wrap = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            wrap.Controls.Add(grid);
            wrap.Controls.Add(btnToggleToken);

            return WrapCard("GitHub 연동", "GitHub 계정을 연결하면 저장소 동기화가 활성화됩니다.", wrap);
        }

        // ───────────────────────────────────────────
        // 섹션: 위험 구역
        // ───────────────────────────────────────────
        private Control BuildSection_DangerZone()
        {
            var stack = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0)
            };

            // 설정 초기화
            var rowReset = MakeDangerRow(
                "모든 설정 초기화",
                "테마·알림·연동 설정을 기본값으로 되돌립니다.",
                "초기화",
                (_, __) =>
                {
                    if (MessageBox.Show("설정을 기본값으로 초기화할까요?", "초기화",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        SettingsService.Reset();
                        (FindForm() as MainForm)?.let(f => Theme.Apply(f));
                        MessageBox.Show("초기화 완료", "완료");
                    }
                });

            // 회원 탈퇴
            var rowDelete = MakeDangerRow(
                "회원 탈퇴",
                "계정과 모든 데이터가 영구적으로 삭제됩니다.",
                "계정 삭제",
                (_, __) =>
                {
                    if (MessageBox.Show("정말로 계정을 삭제하시겠습니까?\n모든 정보가 삭제됩니다.",
                        "경고", MessageBoxButtons.YesNo, MessageBoxIcon.Stop) == DialogResult.Yes)
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
                });

            stack.Controls.Add(rowReset);
            stack.Controls.Add(rowDelete);

            // 위험 구역 카드는 빨간 테두리 강조
            var card = new Panel
            {
                Tag = "card",
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(20, 16, 20, 20)
            };
            Theme.StyleCard(card);
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(220, 80, 60), 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = "⚠  위험 구역",
                Font = new Font("맑은 고딕", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 60, 40),
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 0, 0, 4)
            };
            var lblSub = new Label
            {
                Text = "아래 작업은 되돌릴 수 없습니다.",
                Font = new Font("맑은 고딕", 9f),
                ForeColor = Theme.FgMuted,
                AutoSize = true,
                Tag = "muted",
                Dock = DockStyle.Top,
                Padding = new Padding(0, 0, 0, 10)
            };

            var inner = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 8, 0, 0)
            };
            inner.Controls.Add(lblSub);
            inner.Controls.Add(stack);

            card.Controls.Add(inner);
            card.Controls.Add(lblTitle);

            return card;
        }

        // ───────────────────────────────────────────
        // UI 헬퍼: 토글 행 (체크박스 + 설명)
        // ───────────────────────────────────────────
        private Panel MakeToggleRow(string title, string description, bool isChecked)
        {
            var row = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                Padding = new Padding(0, 6, 0, 6),
                BackColor = Color.Transparent
            };

            var chk = new CheckBox
            {
                Checked = isChecked,
                AutoSize = true,
                Location = new Point(0, 8),
                Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
                ForeColor = Theme.FgDefault,
                Text = title
            };

            var lblDesc = new Label
            {
                Text = description,
                Font = new Font("맑은 고딕", 9f),
                ForeColor = Theme.FgMuted,
                AutoSize = true,
                Location = new Point(22, 30),
                Tag = "muted"
            };

            row.Controls.Add(chk);
            row.Controls.Add(lblDesc);
            return row;
        }

        // ───────────────────────────────────────────
        // UI 헬퍼: 위험 구역 행 (설명 + 버튼)
        // ───────────────────────────────────────────
        private Panel MakeDangerRow(string title, string description, string btnText, EventHandler onClick)
        {
            var row = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                Padding = new Padding(0, 8, 0, 8),
                BackColor = Color.Transparent
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
                ForeColor = Theme.FgDefault,
                AutoSize = true,
                Location = new Point(0, 8)
            };

            var lblDesc = new Label
            {
                Text = description,
                Font = new Font("맑은 고딕", 9f),
                ForeColor = Theme.FgMuted,
                AutoSize = true,
                Location = new Point(0, 30),
                Tag = "muted"
            };

            var btn = new Button
            {
                Text = btnText,
                Width = 100,
                Height = 32,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Theme.StyleDanger(btn);
            btn.Click += onClick;

            row.Resize += (s, e) =>
            {
                btn.Location = new Point(row.Width - btn.Width - 4, (row.Height - btn.Height) / 2);
            };

            row.Controls.Add(lblTitle);
            row.Controls.Add(lblDesc);
            row.Controls.Add(btn);
            return row;
        }
    }

    // 간단한 확장 메서드 (null 체크 + 액션)
    internal static class ObjectExtensions
    {
        public static void let<T>(this T obj, Action<T> action) where T : class
        {
            if (obj != null) action(obj);
        }
    }
}
