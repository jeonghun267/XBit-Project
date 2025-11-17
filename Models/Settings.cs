// Models/Settings.cs

using System;

namespace XBit.Models
{
    public class AppSettings
    {
        public ProfileSettings Profile { get; set; } = new ProfileSettings();
        public AccountSettings Account { get; set; } = new AccountSettings();
        public AppearanceSettings Appearance { get; set; } = new AppearanceSettings();
        public NotificationsSettings Notifications { get; set; } = new NotificationsSettings();
        public PrivacySettings Privacy { get; set; } = new PrivacySettings();
        public SecuritySettings Security { get; set; } = new SecuritySettings();
        public IntegrationsSettings Integrations { get; set; } = new IntegrationsSettings();
    }

    public class ProfileSettings
    {
        public string DisplayName { get; set; } = "»ç¿ëÀÚ";
        public string Email { get; set; } = "";
        public string AvatarPath { get; set; } = "";
    }

    public class AccountSettings
    {
        public string Username { get; set; } = "Local";
        public string Provider { get; set; } = "Local";
    }

    public class AppearanceSettings
    {
        public string Theme { get; set; } = "Light";
    }

    public class NotificationsSettings
    {
        public bool AppUpdates { get; set; } = true;
        public bool AssignmentDue { get; set; } = true;
        public bool PullRequests { get; set; } = true;
        public bool Issues { get; set; } = true;
    }

    public class PrivacySettings
    {
        public bool ShareUsageStats { get; set; } = false;
        public bool ShowEmailInProfile { get; set; } = false;
    }

    public class SecuritySettings
    {
        public bool RequirePasswordOnStart { get; set; } = false;
        public bool BiometricEnabled { get; set; } = false;
    }

    public class IntegrationsSettings
    {
        public string GitHubToken { get; set; } = "";
        public string GitHubUser { get; set; } = "";
        public string LocalRepoPath { get; set; } = @"C:\Users\1\source\repos\X BIT\X BIT";
        public string ClassroomOrg { get; set; } = "jeonghun267";
        public bool UseClassroom { get; set; } = true;
    }
}