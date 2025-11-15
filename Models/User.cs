// XBit/Models/User.cs (전체 코드)

namespace XBit.Models
{
    // ⭐️ 사용자 역할을 정의하는 열거형(Enum)
    public enum UserRole { Student = 0, Admin = 1 }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; }

        // ⭐️ 새로운 속성: 사용자 권한
        public UserRole Role { get; set; }
    }
}