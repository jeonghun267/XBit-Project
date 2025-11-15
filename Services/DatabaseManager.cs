// XBit/Services/DatabaseManager.cs (인덱스 추가)

using System;
using System.Data.SQLite;
using System.IO;
using System.Text;
using XBit.Models;

namespace XBit.Services
{
    public static class DatabaseManager
    {
        private static readonly string DbFile = "Data/xbit.sqlite";
        public static string ConnectionString { get; private set; }

        public static void Initialize()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DbFile);
            ConnectionString = $"Data Source={dbPath};Version=3;";

            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
            }

            CreateTables();
            CreateIndexes(); // ⭐️ 인덱스 생성 추가
            AddInitialDataIfNeeded();
        }

        private static void CreateTables()
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();

                // 1. Users 테이블
                string sqlUsers = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT UNIQUE NOT NULL,
                        PasswordHash TEXT NOT NULL,
                        Name TEXT,
                        Email TEXT,
                        Role INTEGER DEFAULT 0  
                    );";
                using (var cmd = new SQLiteCommand(sqlUsers, conn)) { cmd.ExecuteNonQuery(); }

                // 2. Assignments 테이블
                string sqlAssignments = @"
                    CREATE TABLE IF NOT EXISTS Assignments (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Course TEXT NOT NULL,
                        Title TEXT NOT NULL,
                        DueDate TEXT NOT NULL,
                        Status TEXT NOT NULL,
                        UserId INTEGER,
                        FOREIGN KEY(UserId) REFERENCES Users(Id)
                    );";
                using (var cmd = new SQLiteCommand(sqlAssignments, conn)) { cmd.ExecuteNonQuery(); }

                // 3. Posts 테이블
                string sqlPosts = @"
                    CREATE TABLE IF NOT EXISTS Posts (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        Content TEXT NOT NULL,
                        AuthorId INTEGER NOT NULL,
                        CreatedDate TEXT NOT NULL,
                        FOREIGN KEY(AuthorId) REFERENCES Users(Id)
                    );";
                using (var cmd = new SQLiteCommand(sqlPosts, conn)) { cmd.ExecuteNonQuery(); }

                // 4. Comments 테이블
                string sqlComments = @"
                    CREATE TABLE IF NOT EXISTS Comments (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        PostId INTEGER NOT NULL,
                        AuthorId INTEGER NOT NULL,
                        Content TEXT NOT NULL,
                        CreatedDate TEXT NOT NULL,
                        Likes INTEGER DEFAULT 0,
                        FOREIGN KEY(PostId) REFERENCES Posts(Id),
                        FOREIGN KEY(AuthorId) REFERENCES Users(Id)
                    );";
                using (var cmd = new SQLiteCommand(sqlComments, conn)) { cmd.ExecuteNonQuery(); }

                // 5. Tasks 테이블
                string sqlTasks = @"
                    CREATE TABLE IF NOT EXISTS Tasks (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        Assignee TEXT NOT NULL,
                        Priority INTEGER NOT NULL,
                        Status TEXT NOT NULL,
                        TeamId INTEGER NOT NULL,
                        CreatedDate TEXT NOT NULL
                    );";
                using (var cmd = new SQLiteCommand(sqlTasks, conn)) { cmd.ExecuteNonQuery(); }
            }
        }

        // ⭐️ 인덱스 생성 메서드 추가
        private static void CreateIndexes()
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();

                // Comments 테이블의 PostId에 인덱스 생성 (경고 해결)
                string sqlIndexCommentPostId = @"
                    CREATE INDEX IF NOT EXISTS idx_comments_postid 
                    ON Comments(PostId);";
                using (var cmd = new SQLiteCommand(sqlIndexCommentPostId, conn)) 
                { 
                    cmd.ExecuteNonQuery(); 
                }

                // Comments 테이블의 AuthorId에도 인덱스 추가 (추가 최적화)
                string sqlIndexCommentAuthorId = @"
                    CREATE INDEX IF NOT EXISTS idx_comments_authorid 
                    ON Comments(AuthorId);";
                using (var cmd = new SQLiteCommand(sqlIndexCommentAuthorId, conn)) 
                { 
                    cmd.ExecuteNonQuery(); 
                }

                // Posts 테이블의 AuthorId에 인덱스
                string sqlIndexPostAuthorId = @"
                    CREATE INDEX IF NOT EXISTS idx_posts_authorid 
                    ON Posts(AuthorId);";
                using (var cmd = new SQLiteCommand(sqlIndexPostAuthorId, conn)) 
                { 
                    cmd.ExecuteNonQuery(); 
                }

                // Assignments 테이블의 UserId에 인덱스
                string sqlIndexAssignmentUserId = @"
                    CREATE INDEX IF NOT EXISTS idx_assignments_userid 
                    ON Assignments(UserId);";
                using (var cmd = new SQLiteCommand(sqlIndexAssignmentUserId, conn)) 
                { 
                    cmd.ExecuteNonQuery(); 
                }

                // Tasks 테이블의 TeamId에 인덱스
                string sqlIndexTaskTeamId = @"
                    CREATE INDEX IF NOT EXISTS idx_tasks_teamid 
                    ON Tasks(TeamId);";
                using (var cmd = new SQLiteCommand(sqlIndexTaskTeamId, conn)) 
                { 
                    cmd.ExecuteNonQuery(); 
                }

                // ⭐️ 날짜 기반 검색을 위한 인덱스
                string sqlIndexAssignmentDueDate = @"
                    CREATE INDEX IF NOT EXISTS idx_assignments_duedate 
                    ON Assignments(DueDate);";
                using (var cmd = new SQLiteCommand(sqlIndexAssignmentDueDate, conn)) 
                { 
                    cmd.ExecuteNonQuery(); 
                }

                string sqlIndexPostCreatedDate = @"
                    CREATE INDEX IF NOT EXISTS idx_posts_createddate 
                    ON Posts(CreatedDate);";
                using (var cmd = new SQLiteCommand(sqlIndexPostCreatedDate, conn)) 
                { 
                    cmd.ExecuteNonQuery(); 
                }
            }
        }

        private static void AddInitialDataIfNeeded()
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();

                using (var checkCmd = new SQLiteCommand("SELECT COUNT(1) FROM Users;", conn))
                {
                    object result = null;
                    try { result = checkCmd.ExecuteScalar(); }
                    catch
                    {
                        result = null;
                    }

                    if (result == null || Convert.ToInt32(result) == 0)
                    {
                        // 1. 테스트 관리자 사용자 추가
                        string userSql = "INSERT INTO Users (Username, PasswordHash, Name, Role) VALUES (@u, @p, @n, @r); SELECT last_insert_rowid();";
                        int userId;
                        using (var cmd = new SQLiteCommand(userSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@u", "test");
                            cmd.Parameters.AddWithValue("@p", "1234");
                            cmd.Parameters.AddWithValue("@n", "관리자_정훈");
                            cmd.Parameters.AddWithValue("@r", (int)UserRole.Admin);
                            userId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 2. 테스트 과제 추가
                        string assignmentSql = "INSERT INTO Assignments (Course, Title, DueDate, Status, UserId) VALUES (@c, @t, @d, @s, @uid)";
                        using (var cmd = new SQLiteCommand(assignmentSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@c", "XR Lab");
                            cmd.Parameters.AddWithValue("@t", "포털 UI 프로토 (긴급)");
                            cmd.Parameters.AddWithValue("@d", DateTime.Now.AddHours(12).ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@s", "미제출");
                            cmd.Parameters.AddWithValue("@uid", userId);
                            cmd.ExecuteNonQuery();

                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@c", "Unity3D");
                            cmd.Parameters.AddWithValue("@t", "캐릭터 상호작용");
                            cmd.Parameters.AddWithValue("@d", DateTime.Now.AddDays(3).ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@s", "제출");
                            cmd.Parameters.AddWithValue("@uid", userId);
                            cmd.ExecuteNonQuery();
                        }

                        // 3. 테스트 게시글 추가
                        string postSql = "INSERT INTO Posts (Title, Content, AuthorId, CreatedDate) VALUES (@t, @c, @aid, @cd)";
                        using (var cmd = new SQLiteCommand(postSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@t", "환영합니다! 첫 게시글입니다.");
                            cmd.Parameters.AddWithValue("@c", "XBit 프로젝트 게시판 사용을 시작합니다.");
                            cmd.Parameters.AddWithValue("@aid", userId);
                            cmd.Parameters.AddWithValue("@cd", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }   

        public static string DumpDatabaseInfo()
        {
            return "";
        }
    }
}