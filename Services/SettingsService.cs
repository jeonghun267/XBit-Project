// Services/SettingsService.cs (수정 및 통합 버전)

using Newtonsoft.Json;
using System;
using System.IO;
using System.Xml;
// ⭐️ Theme, AppTheme, AppSettings를 사용하기 위해 XBit 네임스페이스 추가
using XBit;
using XBit.Models;

namespace XBit.Services
{
    // ===============================================
    // ⭐️ AppSettings 모델 정의 (XBit.Models/Settings.cs 또는 여기에 통합)
    // PageSettings.cs에서 참조하는 모든 설정 모델을 정의해야 합니다.
    // ===============================================

    // (이전 답변에서 누락된 AppSettings 및 하위 모델의 최소 정의를 여기에 추가합니다)
    // ⚠️ Note: 실제 프로젝트에서는 XBit.Models 폴더의 Settings.cs에 정의하는 것이 좋습니다.

    // 임시로 SettingsService.cs 파일에 모델을 정의하여 컴파일 오류를 회피합니다.

    public class ProfileSettings { public string DisplayName { get; set; } = "User"; public string Email { get; set; } = ""; public string AvatarPath { get; set; } = ""; }
    public class AccountSettings { public string Username { get; set; } = "Local"; public string Provider { get; set; } = "Local"; }
    public class NotificationsSettings { public bool AppUpdates { get; set; } = true; public bool AssignmentDue { get; set; } = true; public bool PullRequests { get; set; } = true; public bool Issues { get; set; } = true; }
    public class PrivacySettings { public bool ShareUsageStats { get; set; } = false; public bool ShowEmailInProfile { get; set; } = false; }
    public class SecuritySettings { public bool RequirePasswordOnStart { get; set; } = false; public bool BiometricEnabled { get; set; } = false; }
    public class IntegrationsSettings { public string GitHubUser { get; set; } = ""; public string GitHubToken { get; set; } = ""; }

    public class AppSettings
    {
        public ProfileSettings Profile { get; set; } = new ProfileSettings();
        public AccountSettings Account { get; set; } = new AccountSettings();
        public NotificationsSettings Notifications { get; set; } = new NotificationsSettings();
        public PrivacySettings Privacy { get; set; } = new PrivacySettings();
        public SecuritySettings Security { get; set; } = new SecuritySettings();
        public IntegrationsSettings Integrations { get; set; } = new IntegrationsSettings();
        // ⭐️ Theme 속성을 AppTheme 대신 string으로 가정 (로드/저장 로직 기반)
        public string Theme { get; set; } = "Light";
    }

    // ===============================================

    public static class SettingsService
    {
        public static AppSettings Current { get; private set; } = new AppSettings();

        private static string BaseDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "XRHubWin");
        private static string FilePath => Path.Combine(BaseDir, "settings.json");

        public static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    Current = JsonConvert.DeserializeObject<AppSettings>(json)
                              ?? new AppSettings();
                }
                ApplyThemeFromSettings();
            }
            catch
            {
                Current = new AppSettings();
                ApplyThemeFromSettings();
            }
        }

        // Services/SettingsService.cs - Save() 메서드 수정

        public static void Save()
        {
            try
            {
                if (!Directory.Exists(BaseDir)) Directory.CreateDirectory(BaseDir);

                // ⭐️ 오류 해결: Newtonsoft.Json.Formatting으로 명시
                var json = JsonConvert.SerializeObject(Current, Newtonsoft.Json.Formatting.Indented);

                File.WriteAllText(FilePath, json);
            }
            catch { /* 필요시 로깅 */ }
        }

        // ⭐️ SetTheme 메서드 수정: AppTheme 객체를 받아 Current.Theme에 문자열로 저장
        public static void SetTheme(AppTheme theme)
        {
            Current.Theme = (theme == AppTheme.Dark ? "Dark" : "Light");
            Save();
        }

        public static void Reset()
        {
            Current = new AppSettings();
            Save();
            Theme.Set(AppTheme.Light);
        }

        private static void ApplyThemeFromSettings()
        {
            // ⭐️ Theme.Set() 호출 시 Current.Theme (string)을 AppTheme 타입으로 변환
            Theme.Set(string.Equals(Current.Theme, "Dark", StringComparison.OrdinalIgnoreCase)
                ? AppTheme.Dark : AppTheme.Light);
        }
    }
}