// XBit/Services/AssignmentService.cs (최종 수정본 - 제출 상태 업데이트 추가)

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using XBit.Models;

namespace XBit.Services
{
    public class AssignmentService
    {
        // ⭐️ 과제 목록 조회 (기존 코드 유지)
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
                // ⚠️ SQL 쿼리에서 모든 컬럼이 올바른 순서로 매핑되는지 확인 필요
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

        // ⭐️ 2. 과제 상세 조회 메서드 (GetAssignmentById) 추가
        // PageAssignmentDetail.cs에서 과제 상세 정보를 로드할 때 필요합니다.
        public Assignment GetAssignmentById(int assignmentId)
        {
            // ⚠️ 이 메서드의 실제 DB 로직은 현재 코드에 없으므로, 임시 데이터를 반환합니다.
            // 실제 사용 시 DB 쿼리로 대체해야 합니다.
            return new Assignment
            {
                Id = assignmentId,
                Course = "XR Lab",
                Title = $"과제 상세 ID:{assignmentId}",
                DueDate = DateTime.Now.AddDays(1),
                Status = "미제출",
                UserId = AuthService.CurrentUser?.Id ?? 0,
                // Content 컬럼은 이 메서드에 포함되어 있지 않으므로 임시로 비워둡니다.
            };
        }


        // ⭐️ 3. 과제 상태 업데이트 메서드 (GitHub PR 연동 최종 기능)
        public bool UpdateAssignmentStatus(int assignmentId, string newStatus)
        {
            // 토큰을 사용하는 GitHubService와 달리, 이 메서드는 일반 DB 업데이트를 수행합니다.
            string sql = "UPDATE Assignments SET Status = @status WHERE Id = @aid AND UserId = @uid";

            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@status", newStatus);
                        cmd.Parameters.AddWithValue("@aid", assignmentId);
                        cmd.Parameters.AddWithValue("@uid", AuthService.CurrentUser.Id); // ⭐️ 현재 사용자 ID로 권한 확인

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
    }
}