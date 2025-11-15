// XBit/Services/NotificationService.cs

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using XBit.Models;

namespace XBit.Services
{
    public class NotificationService
    {
        // 알림 생성
        public static bool Create(int userId, string title, string message, string type = "System", int? relatedId = null)
        {
            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                try
                {
                    conn.Open();
                    string sql = @"
                        INSERT INTO Notifications (UserId, Title, Message, Type, IsRead, RelatedId, CreatedDate) 
                        VALUES (@uid, @title, @msg, @type, 0, @rid, @date)";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.Parameters.AddWithValue("@title", title);
                        cmd.Parameters.AddWithValue("@msg", message);
                        cmd.Parameters.AddWithValue("@type", type);
                        cmd.Parameters.AddWithValue("@rid", relatedId.HasValue ? (object)relatedId.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        // 사용자의 알림 목록 가져오기
        public List<Notification> GetNotifications(int userId, bool unreadOnly = false)
        {
            var notifications = new List<Notification>();

            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT Id, UserId, Title, Message, Type, IsRead, RelatedId, CreatedDate 
                    FROM Notifications 
                    WHERE UserId = @uid" + (unreadOnly ? " AND IsRead = 0" : "") + @"
                    ORDER BY CreatedDate DESC 
                    LIMIT 50";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            notifications.Add(new Notification
                            {
                                Id = reader.GetInt32(0),
                                UserId = reader.GetInt32(1),
                                Title = reader.GetString(2),
                                Message = reader.GetString(3),
                                Type = reader.GetString(4),
                                IsRead = reader.GetInt32(5) == 1,
                                RelatedId = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                                CreatedDate = DateTime.Parse(reader.GetString(7))
                            });
                        }
                    }
                }
            }

            return notifications;
        }

        // 읽지 않은 알림 개수
        public int GetUnreadCount(int userId)
        {
            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM Notifications WHERE UserId = @uid AND IsRead = 0";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        // 알림 읽음 처리
        public bool MarkAsRead(int notificationId)
        {
            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                try
                {
                    conn.Open();
                    string sql = "UPDATE Notifications SET IsRead = 1 WHERE Id = @id";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", notificationId);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        // 모든 알림 읽음 처리
        public bool MarkAllAsRead(int userId)
        {
            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                try
                {
                    conn.Open();
                    string sql = "UPDATE Notifications SET IsRead = 1 WHERE UserId = @uid AND IsRead = 0";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        // 알림 삭제
        public bool Delete(int notificationId)
        {
            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                try
                {
                    conn.Open();
                    string sql = "DELETE FROM Notifications WHERE Id = @id";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", notificationId);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}