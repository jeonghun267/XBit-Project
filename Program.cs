
using System;
using System.IO;
using System.Windows.Forms;
using XBit;
using XBit.Services;

namespace XBit
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // 전역 예외 처리 등록 (진입점에서 즉시 등록)
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                DatabaseManager.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show("DB 초기화 중 오류가 발생했습니다.\r\n" + ex.Message,
                                "XBit", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                SettingsService.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show("설정을 불러오는 중 오류가 발생했습니다.\r\n" + ex.Message,
                                "XBit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            Application.ApplicationExit += (_, __) =>
            {
                try { SettingsService.Save(); } catch { }
            };

            using (var loginForm = new LoginForm())
            {
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    Application.Run(new MainForm());
                }
            }
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            try
            {
                LogException(e.Exception);
            }
            catch { /* 로깅 실패시 무시 */ }

            try
            {
                MessageBox.Show("오류가 발생했습니다. 로그를 확인해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var ex = e.ExceptionObject as Exception;
                LogException(ex ?? new Exception("Unknown unhandled exception"));
            }
            catch { /* 로깅 실패시 무시 */ }

            try
            {
                MessageBox.Show("오류가 발생했습니다. 로그를 확인해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { }
        }

        private static void LogException(Exception ex)
        {
            try
            {
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string dataDir = Path.Combine(docs, "XBitData");
                if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);

                string logPath = Path.Combine(dataDir, "error_log.txt");
                var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex?.Message}\r\n{ex?.StackTrace}\r\n------------------------\r\n";
                File.AppendAllText(logPath, text);
            }
            catch { /* 로그 실패시 무시 */ }
        }
    }
}