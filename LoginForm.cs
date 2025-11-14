// LoginForm.cs (전체 코드)

using System;
using System.Drawing;
using System.Windows.Forms;
using XBit.Services;
using XBit.Models;

namespace XBit
{
    // 'partial' 키워드는 제거되었고, UI는 코드로 생성됩니다.
    public class LoginForm : Form
    {
        // ⭐️ AuthService 객체를 사용하지 않습니다. (static 호출)

        // 필드 정의: 코드로 생성할 컨트롤
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private Button btnRegister;

        public LoginForm()
        {
            this.Text = "로그인";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            InitializeUIControls();

            // 이벤트 핸들러 연결
            btnLogin.Click += btnLogin_Click;
            btnRegister.Click += btnRegister_Click;
        }

        private void InitializeUIControls()
        {
            // UI 컨트롤 생성 및 배치
            txtUsername = new TextBox { Name = "txtUsername", Text = "사용자 이름", Location = new Point(50, 50), Width = 300, Height = 30 };
            txtPassword = new TextBox { Name = "txtPassword", Text = "비밀번호", UseSystemPasswordChar = true, Location = new Point(50, 90), Width = 300, Height = 30 };
            btnLogin = new Button { Name = "btnLogin", Text = "로그인", Location = new Point(50, 140), Width = 140, Height = 40 };
            btnRegister = new Button { Name = "btnRegister", Text = "회원가입", Location = new Point(210, 140), Width = 140, Height = 40 };

            this.Controls.Add(txtUsername);
            this.Controls.Add(txtPassword);
            this.Controls.Add(btnLogin);
            this.Controls.Add(btnRegister);
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || username == "사용자 이름" || password == "비밀번호")
            {
                MessageBox.Show("아이디와 비밀번호를 입력해주세요.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ⭐️ 수정: 클래스 이름으로 static 메서드 직접 호출
            bool success = AuthService.Login(username, password);

            if (success)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("로그인 정보가 올바르지 않습니다. (비밀번호 오류 또는 사용자 없음)", "오류",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            var registerForm = new RegisterForm();
            this.Hide();
            registerForm.ShowDialog();
            this.Show();
        }
    }
}