// XBit/Services/AssignmentService.cs
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using XBit.Models;

namespace XBit.Services
{
    public class AssignmentService
    {
        public List<Assignment> GetAssignmentsForUser()
        {
            // 현재 로그인된 사용자의 ID를 가져옵니다. (로그인되지 않았다면 빈 목록 반환)
            int userId = AuthService.CurrentUser?.Id ?? -1;
            if (userId == -1) return new List<Assignment>();

            var assignments = new List<Assignment>();

            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                conn.Open();
                // 로그인된 사용자의 과제만 가져옵니다.
                string sql = "SELECT Id, Course, Title, DueDate, Status, UserId FROM Assignments WHERE UserId = @uid";

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
                                // DB의 TEXT를 DateTime으로 변환
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
    }
}