// Program.cs
using System;
using System.Windows.Forms;
using XBit;                 // LoginForm, MainForm
using XBit.Services;        // SettingsService, DatabaseManager

namespace XBit
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 1. DB 초기화 및 연결 설정
            try
            {
                DatabaseManager.Initialize();

                // 진단: 런타임 DB 상태를 즉시 표시합니다. 문제 확인 후 제거하세요.
                try
                {
                    MessageBox.Show(DatabaseManager.DumpDatabaseInfo(), "DB Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch { /* 무시: 진단 호출 실패 시 앱 정상 실행은 계속 */ }
            }
            catch (Exception ex)
            {
                MessageBox.Show("DB 초기화 중 오류가 발생했습니다.\r\n" + ex.Message,
                                "XBit", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 2. 설정 로드 (기존 로직 유지)
            try { SettingsService.Load(); }
            catch (Exception ex)
            {
                MessageBox.Show("설정을 불러오는 중 오류가 발생했습니다.\r\n" + ex.Message,
                                "XBit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // 3. 앱 종료 시 설정 저장
            Application.ApplicationExit += (_, __) =>
            {
                try { SettingsService.Save(); } catch { /* 필요 시 로깅 */ }
            };

            // 4. 로그인 폼 실행 및 결과 확인
            using (var loginForm = new LoginForm())
            {
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    Application.Run(new MainForm());
                }
            }
        }
    }
}