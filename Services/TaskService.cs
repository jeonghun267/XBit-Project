// XBit/Services/TaskService.cs

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using XBit.Models;

namespace XBit.Services
{
    public class TaskService
    {
        public List<ProjectTask> GetTasksByTeam(int teamId)
        {
            var tasks = new List<ProjectTask>();

            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                conn.Open();
                string sql = "SELECT Id, Title, Assignee, Priority, Status, TeamId, CreatedDate FROM Tasks WHERE TeamId = @tid ORDER BY Priority ASC";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@tid", teamId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tasks.Add(new ProjectTask
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Assignee = reader.GetString(2),
                                Priority = reader.GetInt32(3),
                                Status = reader.GetString(4),
                                TeamId = reader.GetInt32(5),
                                CreatedDate = DateTime.Parse(reader.GetString(6))
                            });
                        }
                    }
                }
            }

            return tasks;
        }

        public bool UpdateTaskStatus(int taskId, string newStatus)
        {
            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                try
                {
                    conn.Open();
                    string sql = "UPDATE Tasks SET Status = @status WHERE Id = @id";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@status", newStatus);
                        cmd.Parameters.AddWithValue("@id", taskId);

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
                catch (SQLiteException ex)
                {
                    System.Diagnostics.Debug.WriteLine("UpdateTaskStatus failed: " + ex.Message);
                    return false;
                }
            }
        }

        public bool AddTask(string title, string assignee, int priority, string status, int teamId)
        {
            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                try
                {
                    conn.Open();
                    string sql = "INSERT INTO Tasks (Title, Assignee, Priority, Status, TeamId, CreatedDate) VALUES (@title, @assignee, @priority, @status, @teamId, @created)";

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
                catch (SQLiteException ex)
                {
                    System.Diagnostics.Debug.WriteLine("AddTask failed: " + ex.Message);
                    return false;
                }
            }
        }
    }
}