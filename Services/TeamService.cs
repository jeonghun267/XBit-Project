// XBit/Services/TeamService.cs

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using XBit.Models;

namespace XBit.Services
{
    public class TeamService
    {
        // 팀 생성
        public int CreateTeam(string teamName, int ownerId)
        {
            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                conn.Open();
                
                // 1. 팀 생성
                string sql = "INSERT INTO Teams (Name, OwnerId, CreatedDate) VALUES (@name, @owner, @date); SELECT last_insert_rowid();";
                int teamId;
                
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@name", teamName);
                    cmd.Parameters.AddWithValue("@owner", ownerId);
                    cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    teamId = Convert.ToInt32(cmd.ExecuteScalar());
                }
                
                // 2. 생성자를 Owner로 추가
                AddMember(teamId, ownerId, "Owner");
                
                return teamId;
            }
        }

        // 팀 목록 가져오기 (사용자가 속한 팀)
        public List<Team> GetTeamsByUser(int userId)
        {
            var teams = new List<Team>();
            
            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT DISTINCT t.Id, t.Name, t.OwnerId, t.CreatedDate
                    FROM Teams t
                    INNER JOIN TeamMembers tm ON t.Id = tm.TeamId
                    WHERE tm.UserId = @uid
                    ORDER BY t.CreatedDate DESC";
                
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            teams.Add(new Team
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                OwnerId = reader.GetInt32(2),
                                CreatedDate = DateTime.Parse(reader.GetString(3))
                            });
                        }
                    }
                }
            }
            
            return teams;
        }

        // 멤버 추가
        public bool AddMember(int teamId, int userId, string role = "Member")
        {
            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                try
                {
                    conn.Open();
                    string sql = "INSERT INTO TeamMembers (TeamId, UserId, Role, JoinedDate) VALUES (@tid, @uid, @role, @date)";
                    
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@tid", teamId);
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.Parameters.AddWithValue("@role", role);
                        cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        
                        cmd.ExecuteNonQuery();
                    }
                    
                    // ?? 알림 생성
                    string teamName = GetTeamName(teamId);
                    NotificationService.Create(
                        userId, 
                        "팀 초대", 
                        $"'{teamName}' 팀에 초대되었습니다!",
                        "Team",
                        teamId
                    );
                    
                    return true;
                }
                catch (SQLiteException)
                {
                    return false;
                }
            }
        }

        private string GetTeamName(int teamId)
        {
            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                conn.Open();
                string sql = "SELECT Name FROM Teams WHERE Id = @tid";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@tid", teamId);
                    return cmd.ExecuteScalar()?.ToString() ?? "알 수 없는 팀";
                }
            }
        }

        // 팀 멤버 목록
        public List<TeamMember> GetTeamMembers(int teamId)
        {
            var members = new List<TeamMember>();
            
            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT tm.Id, tm.TeamId, tm.UserId, u.Username, u.Name, tm.Role, tm.JoinedDate
                    FROM TeamMembers tm
                    INNER JOIN Users u ON tm.UserId = u.Id
                    WHERE tm.TeamId = @tid
                    ORDER BY 
                        CASE tm.Role
                            WHEN 'Owner' THEN 1
                            WHEN 'Leader' THEN 2
                            ELSE 3
                        END,
                        tm.JoinedDate";
                
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@tid", teamId);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            members.Add(new TeamMember
                            {
                                Id = reader.GetInt32(0),
                                TeamId = reader.GetInt32(1),
                                UserId = reader.GetInt32(2),
                                Username = reader.GetString(3),
                                Name = reader.IsDBNull(4) ? null : reader.GetString(4),
                                Role = reader.GetString(5),
                                JoinedDate = DateTime.Parse(reader.GetString(6))
                            });
                        }
                    }
                }
            }
            
            return members;
        }

        // 멤버 제거
        public bool RemoveMember(int teamId, int userId)
        {
            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                try
                {
                    conn.Open();
                    string sql = "DELETE FROM TeamMembers WHERE TeamId = @tid AND UserId = @uid AND Role != 'Owner'";
                    
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@tid", teamId);
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

        // 팀 삭제
        public bool DeleteTeam(int teamId, int requestUserId)
        {
            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                try
                {
                    conn.Open();
                    
                    // Owner만 삭제 가능
                    string checkSql = "SELECT OwnerId FROM Teams WHERE Id = @tid";
                    using (var cmd = new SQLiteCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@tid", teamId);
                        var ownerId = cmd.ExecuteScalar();
                        
                        if (ownerId == null || Convert.ToInt32(ownerId) != requestUserId)
                            return false;
                    }
                    
                    // 팀 삭제 (CASCADE로 멤버도 자동 삭제)
                    string sql = "DELETE FROM Teams WHERE Id = @tid";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@tid", teamId);
                        cmd.ExecuteNonQuery();
                    }
                    
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}