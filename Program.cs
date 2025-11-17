// Program.cs
using System;
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
    }
}