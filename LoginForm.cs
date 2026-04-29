using System;
using System.Drawing;
using System.Windows.Forms;
using XBit.Services;
using XBit.Models;

namespace XBit
{
    public class LoginForm : Form
    {
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private Button btnRegister;

        // 커스텀 디자인을 위한 패널들
        private Panel pnlUserUnderline;
        private Panel pnlPassUnderline;

        public LoginForm()
        {
            this.Text = "XBit Login";
            this.Size = new Size(400, 550); // 폼 크기는 유지하되
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // [배경 설정]
            try
            {
                this.BackgroundImage = global::X_BIT.Properties.Resources.LoginBg;
                this.BackgroundImageLayout = ImageLayout.Stretch;
            }
            catch
            {
                this.BackColor = Color.FromArgb(20, 20, 25);
            }

            InitializeUIControls();

            btnLogin.Click += btnLogin_Click;
            btnRegister.Click += btnRegister_Click;
        }

        private void InitializeUIControls()
        {
            txtUsername = CreateStyledTextBox("사용자 이름", 120, false);
            pnlUserUnderline = CreateUnderline(txtUsername, Color.Cyan);

            txtPassword = CreateStyledTextBox("비밀번호", 180, true);
            pnlPassUnderline = CreateUnderline(txtPassword, Color.Magenta);

            // 2. 버튼 위치도 따라서 올림
            btnLogin = new Button
            {
                Text = "LOGIN",
                Location = new Point(50, 260), // 기존 340 -> 260
                Width = 300,
                Height = 50,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(20, 255, 255, 255),
                ForeColor = Color.Cyan,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 1;
            btnLogin.FlatAppearance.BorderColor = Color.Cyan;
            btnLogin.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 0, 255, 255);

            btnRegister = new Button
            {
                Text = "Create Account",
                Location = new Point(50, 320), // 기존 410 -> 320
                Width = 300,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand
            };
            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 255, 255, 255);

            // 컨트롤 추가
            this.Controls.Add(txtUsername);
            this.Controls.Add(pnlUserUnderline);

            this.Controls.Add(txtPassword);
            this.Controls.Add(pnlPassUnderline);

            this.Controls.Add(btnLogin);
            this.Controls.Add(btnRegister);

            // 포커스 효과
            AddFocusEffect(txtUsername, pnlUserUnderline, Color.Cyan);
            AddFocusEffect(txtPassword, pnlPassUnderline, Color.Magenta);
        }

        // 헬퍼: 투명 배경 느낌의 텍스트박스 생성
        private TextBox CreateStyledTextBox(string placeholder, int y, bool isPassword)
        {
            var tb = new TextBox
            {
                // 비밀번호 필드: 처음부터 비어 있고 바로 점(●) 모드
                // 일반 필드: placeholder 텍스트 표시
                Text = isPassword ? "" : placeholder,
                Location = new Point(50, y),
                Width = 300,
                Height = 30,
                Font = new Font("맑은 고딕", 11),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = isPassword ? Color.White : Color.Gray,
                UseSystemPasswordChar = isPassword  // 비밀번호 필드는 항상 ● 모드 유지
            };

            if (!isPassword)
            {
                // 일반 필드만 placeholder 텍스트 토글
                tb.GotFocus += (s, e) =>
                {
                    if (tb.Text == placeholder)
                    {
                        tb.Text = "";
                        tb.ForeColor = Color.White;
                    }
                };
                tb.LostFocus += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(tb.Text))
                    {
                        tb.Text = placeholder;
                        tb.ForeColor = Color.Gray;
                    }
                };
            }

            return tb;
        }

        // 헬퍼: 밑줄 패널 생성
        private Panel CreateUnderline(Control target, Color color)
        {
            return new Panel
            {
                Location = new Point(target.Left, target.Bottom + 5),
                Width = target.Width,
                Height = 2,
                BackColor = Color.Gray
            };
        }

        // 헬퍼: 포커스 시 밑줄 색상 변경
        private void AddFocusEffect(TextBox tb, Panel underline, Color activeColor)
        {
            tb.GotFocus += (s, e) => { underline.BackColor = activeColor; underline.Height = 3; };
            tb.LostFocus += (s, e) => { underline.BackColor = Color.Gray; underline.Height = 2; };
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            if (username == "사용자 이름" || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("로그인 정보를 입력하세요.", "알림");
                return;
            }

            if (AuthService.Login(username, password))
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("로그인 실패", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            var reg = new RegisterForm();
            this.Hide();
            reg.ShowDialog();
            this.Show();
        }
    }
}