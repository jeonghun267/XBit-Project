using System;
using System.Collections.Generic;
using System.Data.SQLite;
using XBit.Models;

namespace XBit.Services
{
    public class TaskRepository
    {
        public List<ProjectTask> GetTasksByTeam(int teamId)
        {
            var tasks = new List<ProjectTask>();

            try
            {
                using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
                {
                    conn.Open();
                    const string sql = "SELECT Id, Title, Assignee, Priority, Status, TeamId, CreatedDate FROM Tasks WHERE TeamId = @tid ORDER BY Priority ASC";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@tid", teamId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var createdStr = reader.IsDBNull(6) ? null : reader.GetString(6);
                                DateTime created = DateTime.MinValue;
                                if (!string.IsNullOrEmpty(createdStr))
                                {
                                    DateTime.TryParse(createdStr, out created);
                                }

                                tasks.Add(new ProjectTask
                                {
                                    Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                                    Title = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                    Assignee = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                    Priority = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                                    Status = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                    TeamId = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                                    CreatedDate = created
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TaskRepository] GetTasksByTeam 예외: {ex.Message}");
            }

            return tasks;
        }

        public bool UpdateTaskStatus(int taskId, string newStatus)
        {
            if (taskId <= 0) return false;
            if (newStatus == null) newStatus = string.Empty;

            try
            {
                using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
                {
                    conn.Open();
                    const string sql = "UPDATE Tasks SET Status = @status WHERE Id = @id";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@status", newStatus);
                        cmd.Parameters.AddWithValue("@id", taskId);

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (SQLiteException ex)
            {
                System.Diagnostics.Debug.WriteLine("[TaskRepository] UpdateTaskStatus failed: " + ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[TaskRepository] UpdateTaskStatus unexpected: " + ex.Message);
                return false;
            }
        }

        public bool AddTask(string title, string assignee, int priority, string status, int teamId)
        {
            if (string.IsNullOrWhiteSpace(title)) return false;
            if (string.IsNullOrWhiteSpace(assignee)) assignee = "Unassigned";
            if (priority < 0) priority = 0;
            if (string.IsNullOrWhiteSpace(status)) status = "New";
            if (teamId <= 0) return false;

            try
            {
                using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
                {
                    conn.Open();
                    const string sql = "INSERT INTO Tasks (Title, Assignee, Priority, Status, TeamId, CreatedDate) VALUES (@title, @assignee, @priority, @status, @teamId, @created)";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@title", title);
                        cmd.Parameters.AddWithValue("@assignee", assignee);
                        cmd.Parameters.AddWithValue("@priority", priority);
                        cmd.Parameters.AddWithValue("@status", status);
                        cmd.Parameters.AddWithValue("@teamId", teamId);
                        cmd.Parameters.AddWithValue("@created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (SQLiteException ex)
            {
                System.Diagnostics.Debug.WriteLine("[TaskRepository] AddTask failed: " + ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[TaskRepository] AddTask unexpected: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 팀별 전체 작업 수와 완료 작업 수를 반환 (완료율 계산용)
        /// </summary>
        public (int total, int completed) GetTaskCompletionStatsByTeam(int teamId)
        {
            int total = 0;
            int completed = 0;

            try
            {
                using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
                {
                    conn.Open();
                    const string sql = @"
                        SELECT 
                            COUNT(1) AS Total,
                            SUM(CASE 
                                WHEN LOWER(Status) = '완료' OR LOWER(Status) = 'done' OR Status LIKE '%완료%' THEN 1
                                ELSE 0
                            END) AS Completed
                        FROM Tasks
                        WHERE TeamId = @tid
                    ";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@tid", teamId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                total = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                                completed = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TaskRepository] GetTaskCompletionStatsByTeam 예외: {ex.Message}");
            }

            return (total, completed);
        }
    }
}