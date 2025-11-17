// Services/SettingsService.cs

using Newtonsoft.Json;
using System;
using System.IO;
using XBit.Models;

namespace XBit.Services
{
    public static class SettingsService
    {
        private static readonly string SettingsFilePath = "Data/settings.json";
        private static AppSettings _currentSettings;

        public static AppSettings Current
        {
            get
            {
                if (_currentSettings == null)
                {
                    _currentSettings = Load();
                }
                return _currentSettings;
            }
        }

        public static void Initialize()
        {
            System.Diagnostics.Debug.WriteLine("[SettingsService] 초기화 시작");
            _currentSettings = Load();
            
            // Theme 초기화
            ApplyTheme();
        }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    System.Diagnostics.Debug.WriteLine("[SettingsService] 설정 로드 성공");
                    return settings ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsService] 설정 로드 실패: {ex.Message}");
            }

            return new AppSettings();
        }

        public static void Save()
        {
            Save(_currentSettings);
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string json = JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(SettingsFilePath, json);

                _currentSettings = settings;
                System.Diagnostics.Debug.WriteLine("[SettingsService] 설정 저장 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsService] 설정 저장 실패: {ex.Message}");
            }
        }

        public static void SetTheme(string theme)
        {
            Current.Appearance.Theme = theme;
            ApplyTheme();
            Save();
        }

        public static void SetTheme(AppTheme theme)
        {
            Current.Appearance.Theme = theme.ToString();
            ApplyTheme();
            Save();
        }

        private static void ApplyTheme()
        {
            string themeName = Current.Appearance.Theme;
            
            if (Enum.TryParse<AppTheme>(themeName, out AppTheme themeEnum))
            {
                Theme.Set(themeEnum);
                System.Diagnostics.Debug.WriteLine($"[SettingsService] 테마 적용: {themeEnum}");
            }
            else
            {
                Theme.Set(AppTheme.Light);
                System.Diagnostics.Debug.WriteLine("[SettingsService] 기본 테마 적용: Light");
            }
        }

        public static void Reset()
        {
            _currentSettings = new AppSettings();
            ApplyTheme();
            Save();
        }
    }
}