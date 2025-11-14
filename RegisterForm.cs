// RegisterForm.cs (전체 코드)

using System;
using System.Windows.Forms;
using System.Drawing;
using XBit.Services;
using XBit.Models;
using System.Security.Cryptography; // 참조 유지를 위해 추가
using System.Text;

namespace XBit
{
    public class RegisterForm : Form
    {
        private TextBox txtUsername;
        private TextBox txtPassword;
        private TextBox txtName;
        private TextBox txtEmail;
        private Button btnRegister;
        private Button btnCancel;

        public RegisterForm()
        {
            this.Text = "회원가입";
            this.Size = new Size(450, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;

            InitializeRegisterUI();
        }

        private void InitializeRegisterUI()
        {
            var layoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(30),
                ColumnCount = 2,
                RowCount = 6,
                AutoSize = true
            };
            layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // 제목 라벨
            layoutPanel.Controls.Add(new Label { Text = "회원가입을 진행합니다", Font = new Font("Segoe UI", 12f, FontStyle.Bold), AutoSize = true }, 0, 0);

            // UI 컨트롤 생성 및 배치
            txtUsername = new TextBox { Dock = DockStyle.Fill, Height = 25 };
            txtPassword = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true, Height = 25 };
            txtName = new TextBox { Dock = DockStyle.Fill, Height = 25 };
            txtEmail = new TextBox { Dock = DockStyle.Fill, Height = 25 };

            // UI 배치
            layoutPanel.Controls.Add(new Label { Text = "사용자 이름", AutoSize = true, TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            layoutPanel.Controls.Add(txtUsername, 1, 1);
            layoutPanel.Controls.Add(new Label { Text = "비밀번호", AutoSize = true, TextAlign = ContentAlignment.MiddleRight }, 0, 2);
            layoutPanel.Controls.Add(txtPassword, 1, 2);
            layoutPanel.Controls.Add(new Label { Text = "이름", AutoSize = true, TextAlign = ContentAlignment.MiddleRight }, 0, 3);
            layoutPanel.Controls.Add(txtName, 1, 3);
            layoutPanel.Controls.Add(new Label { Text = "이메일", AutoSize = true, TextAlign = ContentAlignment.MiddleRight }, 0, 4);
            layoutPanel.Controls.Add(txtEmail, 1, 4);

            // 버튼 영역
            btnRegister = new Button { Text = "가입하기", Height = 35, Width = 100 };
            btnCancel = new Button { Text = "취소", Height = 35, Width = 80 };

            var pnlButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 20, 0, 0) };
            pnlButtons.Controls.Add(btnCancel);
            pnlButtons.Controls.Add(btnRegister);
            layoutPanel.Controls.Add(pnlButtons, 1, 5);

            this.Controls.Add(layoutPanel);

            // 이벤트 연결
            btnRegister.Click += BtnRegister_Click;
            btnCancel.Click += (s, e) => this.Close();
        }

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();
            string name = txtName.Text.Trim();
            string email = txtEmail.Text.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("사용자 이름과 비밀번호는 필수 입력 항목입니다.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // DB에 새 사용자 정보를 저장하는 로직 호출 (AuthService는 static)
            string errorMessage = AuthService.Register(new User
            {
                Username = username,
                Name = name,
                Email = email,
            }, password);

            if (errorMessage == null)
            {
                MessageBox.Show("회원가입이 완료되었습니다. 로그인 화면으로 돌아갑니다.", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close(); // 로그인 폼으로 돌아감
            }
            else
            {
                // ⭐️ 반환된 구체적인 오류 메시지를 사용자에게 표시
                MessageBox.Show(errorMessage, "회원가입 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
} 