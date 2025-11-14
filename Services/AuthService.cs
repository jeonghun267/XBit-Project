// XBit/Services/AuthService.cs (최종 완성본)

using System;
using System.Data.SQLite;
using XBit.Models;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace XBit.Services
{
    public class AuthService
    {
        public static User CurrentUser { get; private set; }

        private static string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
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
                string sql = "SELECT Id, Username, PasswordHash, Name, Email, Role FROM Users WHERE Username = @u AND PasswordHash = @p";

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
                                PasswordHash = reader.GetString(2),
                                Name = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                                // UserRole 로드
                                Role = (UserRole)reader.GetInt32(5)
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

        public static string Register(User newUser, string rawPassword)
        {
            newUser.PasswordHash = HashPassword(rawPassword);

            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                try
                {
                    conn.Open();
                    string sql = "INSERT INTO Users (Username, PasswordHash, Name, Email, Role) VALUES (@u, @p, @n, @e, @r)";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@u", newUser.Username);
                        cmd.Parameters.AddWithValue("@p", newUser.PasswordHash);
                        cmd.Parameters.AddWithValue("@n", newUser.Name ?? "");
                        cmd.Parameters.AddWithValue("@e", newUser.Email ?? "");
                        // 회원가입 시 기본 Role은 Student(0)
                        cmd.Parameters.AddWithValue("@r", (int)UserRole.Student);
                        cmd.ExecuteNonQuery();
                    }
                    return null;
                }
                catch (SQLiteException ex)
                {
                    if (ex.Message.Contains("UNIQUE constraint failed"))
                        return "오류: 사용자 이름이 이미 존재합니다.";
                    if (ex.Message.Contains("NOT NULL constraint failed"))
                        return "오류: 필수 입력 항목을 비워두었습니다.";
                    return "DB 오류: " + ex.Message;
                }
            }
        }

        public static bool UpdateUser(User user)
        {
            if (user == null || CurrentUser == null || user.Id != CurrentUser.Id) return false;

            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                try
                {
                    conn.Open();
                    string sql = "UPDATE Users SET Name = @n, Email = @e WHERE Id = @id";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@n", user.Name);
                        cmd.Parameters.AddWithValue("@e", user.Email);
                        cmd.Parameters.AddWithValue("@id", user.Id);
                        cmd.ExecuteNonQuery();
                    }

                    CurrentUser.Name = user.Name;
                    CurrentUser.Email = user.Email;

                    return true;
                }
                catch (SQLiteException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateUser failed: {ex.Message}");
                    return false;
                }
            }
        }

        // ⭐️ DeleteUser 메서드 수정: 게시물은 유지하고 사용자 정보만 삭제하는 '익명화 정책' 반영
        public static bool DeleteUser(int userId)
        {
            if (userId <= 0) return false;

            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                try
                {
                    conn.Open();
                    // 사용자 정보만 삭제 (게시물은 남아 익명 처리됨)
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