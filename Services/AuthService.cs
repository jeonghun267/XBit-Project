// XBit/Services/AuthService.cs (최종 완성본)

using System;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;
using XBit.Models;

namespace XBit.Services
{
    public static class AuthService
    {
        public static User CurrentUser { get; private set; }

        // ⭐️ SHA256 해싱 메서드
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static bool Login(string username, string password)
        {
            string hashedPassword = HashPassword(password);
            
            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                conn.Open();
                string sql = "SELECT Id, Username, Name, Email, Role FROM Users WHERE Username = @u AND PasswordHash = @p";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@u", username);
                    cmd.Parameters.AddWithValue("@p", hashedPassword);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            CurrentUser = new User
                            {
                                Id = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Name = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Role = (UserRole)reader.GetInt32(4)
                            };
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static void Logout()
        {
            CurrentUser = null;
        }

        public static string Register(User user, string password)
        {
            string hashedPassword = HashPassword(password);
            
            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                try
                {
                    conn.Open();
                    string sql = "INSERT INTO Users (Username, PasswordHash, Name, Email, Role) VALUES (@u, @p, @n, @e, @r)";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@u", user.Username);
                        cmd.Parameters.AddWithValue("@p", hashedPassword);
                        cmd.Parameters.AddWithValue("@n", user.Name ?? string.Empty);
                        cmd.Parameters.AddWithValue("@e", user.Email ?? string.Empty);
                        cmd.Parameters.AddWithValue("@r", (int)user.Role);

                        cmd.ExecuteNonQuery();
                    }

                    return null;
                }
                catch (SQLiteException ex)
                {
                    if (ex.Message.Contains("UNIQUE"))
                    {
                        return "이미 존재하는 사용자 이름입니다.";
                    }
                    return "회원가입 실패: " + ex.Message;
                }
            }
        }

        public static User GetUserByEmail(string email)
        {
            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                conn.Open();
                string sql = "SELECT Id, Username, Name, Email, Role FROM Users WHERE Email = @e";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@e", email);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new User
                            {
                                Id = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Name = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Role = (UserRole)reader.GetInt32(4)
                            };
                        }
                    }
                }
            }
            return null;
        }

        public static bool UpdateUser(User user)
        {
            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                try
                {
                    conn.Open();
                    string sql = "UPDATE Users SET Name = @n, Email = @e WHERE Id = @id";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@n", user.Name ?? string.Empty);
                        cmd.Parameters.AddWithValue("@e", user.Email ?? string.Empty);
                        cmd.Parameters.AddWithValue("@id", user.Id);

                        cmd.ExecuteNonQuery();
                    }

                    if (CurrentUser != null && CurrentUser.Id == user.Id)
                    {
                        CurrentUser = user;
                    }

                    return true;
                }
                catch (SQLiteException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateUser failed: {ex.Message}");
                    return false;
                }
            }
        }

        public static bool DeleteUser(int userId)
        {
            if (userId <= 0) return false;

            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                try
                {
                    conn.Open();
                    string sql = "DELETE FROM Users WHERE Id = @id";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", userId);
                        cmd.ExecuteNonQuery();
                    }

                    return true;
                }
                catch (SQLiteException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DeleteUser failed: {ex.Message}");
                    return false;
                }
            }
        }
    }
}