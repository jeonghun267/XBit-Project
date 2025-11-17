// XBit/Services/AssignmentService.cs (실제 DB 연동)

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using XBit.Models;

namespace XBit.Services
{
    public class AssignmentService
    {
        // ⭐️ 실제 DB에서 사용자의 프로젝트 목록 가져오기
        public List<Assignment> GetAssignmentsForUser(int userId)
        {
            var assignments = new List<Assignment>();

            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT Id, Course, Title, DueDate, Status, UserId 
                    FROM Assignments 
                    WHERE UserId = @uid 
                    ORDER BY DueDate ASC";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            assignments.Add(new Assignment
                            {
                                Id = reader.GetInt32(0),
                                Course = reader.GetString(1),
                                Title = reader.GetString(2),
                                DueDate = DateTime.Parse(reader.GetString(3)),
                                Status = reader.GetString(4),
                                UserId = reader.GetInt32(5)
                            });
                        }
                    }
                }
            }

            return assignments;
        }

        // ⭐️ 실제 DB에서 프로젝트 상세 정보 가져오기
        public Assignment GetAssignmentById(int assignmentId)
        {
            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT Id, Course, Title, DueDate, Status, UserId 
                    FROM Assignments 
                    WHERE Id = @aid";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@aid", assignmentId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Assignment
                            {
                                Id = reader.GetInt32(0),
                                Course = reader.GetString(1),
                                Title = reader.GetString(2),
                                DueDate = DateTime.Parse(reader.GetString(3)),
                                Status = reader.GetString(4),
                                UserId = reader.GetInt32(5)
                            };
                        }
                    }
                }
            }

            return null;
        }

        // ⭐️ 프로젝트 상태 업데이트
        public bool UpdateAssignmentStatus(int assignmentId, string newStatus)
        {
            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                try
                {
                    conn.Open();
                    string sql = "UPDATE Assignments SET Status = @status WHERE Id = @aid AND UserId = @uid";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@status", newStatus);
                        cmd.Parameters.AddWithValue("@aid", assignmentId);
                        cmd.Parameters.AddWithValue("@uid", AuthService.CurrentUser.Id);

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
                catch (SQLiteException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateAssignmentStatus failed: {ex.Message}");
                    return false;
                }
            }
        }

        // ⭐️ 새 프로젝트 추가
        public bool AddAssignment(string course, string title, DateTime dueDate, int userId)
        {
            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                try
                {
                    conn.Open();
                    string sql = @"
                        INSERT INTO Assignments (Course, Title, DueDate, Status, UserId) 
                        VALUES (@c, @t, @d, @s, @uid)";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@c", course);
                        cmd.Parameters.AddWithValue("@t", title);
                        cmd.Parameters.AddWithValue("@d", dueDate.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@s", "미제출");
                        cmd.Parameters.AddWithValue("@uid", userId);

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
                catch (SQLiteException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AddAssignment failed: {ex.Message}");
                    return false;
                }
            }
        }

        // ⭐️ 프로젝트 삭제
        public bool DeleteAssignment(int assignmentId, int userId)
        {
            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                try
                {
                    conn.Open();
                    string sql = "DELETE FROM Assignments WHERE Id = @aid AND UserId = @uid";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@aid", assignmentId);
                        cmd.Parameters.AddWithValue("@uid", userId);

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
                catch (SQLiteException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DeleteAssignment failed: {ex.Message}");
                    return false;
                }
            }
        }
    }
}