// XBit/Services/DatabaseManager.cs (디버그 로그 추가 버전)

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

            System.Diagnostics.Debug.WriteLine($"[DB] 초기화 시작 - 경로: {dbPath}");

            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                System.Diagnostics.Debug.WriteLine($"[DB] 디렉터리 생성: {dir}");
            }

            bool isNewFile = !File.Exists(dbPath);
            if (isNewFile)
            {
                SQLiteConnection.CreateFile(dbPath);
                System.Diagnostics.Debug.WriteLine("[DB] 새 DB 파일 생성됨!");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[DB] 기존 DB 파일 사용");
            }

            CreateTables();
            CreateIndexes();
            AddInitialDataIfNeeded();
            
            System.Diagnostics.Debug.WriteLine("[DB] 초기화 완료!");
        }

        private static void CreateTables()
        {
            System.Diagnostics.Debug.WriteLine("[DB] 테이블 생성 시작...");
            
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

                // 6. Teams 테이블
                string sqlTeams = @"
                    CREATE TABLE IF NOT EXISTS Teams (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        OwnerId INTEGER NOT NULL,
                        CreatedDate TEXT NOT NULL,
                        FOREIGN KEY(OwnerId) REFERENCES Users(Id)
                    );";
                using (var cmd = new SQLiteCommand(sqlTeams, conn)) { cmd.ExecuteNonQuery(); }

                // 7. TeamMembers 테이블
                string sqlTeamMembers = @"
                    CREATE TABLE IF NOT EXISTS TeamMembers (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        TeamId INTEGER NOT NULL,
                        UserId INTEGER NOT NULL,
                        Role TEXT DEFAULT 'Member',
                        JoinedDate TEXT NOT NULL,
                        FOREIGN KEY(TeamId) REFERENCES Teams(Id),
                        FOREIGN KEY(UserId) REFERENCES Users(Id),
                        UNIQUE(TeamId, UserId)
                    );";
                using (var cmd = new SQLiteCommand(sqlTeamMembers, conn)) { cmd.ExecuteNonQuery(); }

                // 8. Notifications 테이블
                string sqlNotifications = @"
                    CREATE TABLE IF NOT EXISTS Notifications (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId INTEGER NOT NULL,
                        Title TEXT NOT NULL,
                        Message TEXT NOT NULL,
                        Type TEXT NOT NULL,
                        IsRead INTEGER DEFAULT 0,
                        RelatedId INTEGER,
                        CreatedDate TEXT NOT NULL,
                        FOREIGN KEY(UserId) REFERENCES Users(Id)
                    );";
                using (var cmd = new SQLiteCommand(sqlNotifications, conn)) { cmd.ExecuteNonQuery(); }
            }
            
            System.Diagnostics.Debug.WriteLine("[DB] 테이블 생성 완료!");
        }

        private static void CreateIndexes()
        {
            System.Diagnostics.Debug.WriteLine("[DB] 인덱스 생성 시작...");
            
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();

                string[] indexes = {
                    "CREATE INDEX IF NOT EXISTS idx_comments_postid ON Comments(PostId);",
                    "CREATE INDEX IF NOT EXISTS idx_comments_authorid ON Comments(AuthorId);",
                    "CREATE INDEX IF NOT EXISTS idx_posts_authorid ON Posts(AuthorId);",
                    "CREATE INDEX IF NOT EXISTS idx_assignments_userid ON Assignments(UserId);",
                    "CREATE INDEX IF NOT EXISTS idx_tasks_teamid ON Tasks(TeamId);",
                    "CREATE INDEX IF NOT EXISTS idx_assignments_duedate ON Assignments(DueDate);",
                    "CREATE INDEX IF NOT EXISTS idx_posts_createddate ON Posts(CreatedDate);",
                    "CREATE INDEX IF NOT EXISTS idx_teammembers_teamid ON TeamMembers(TeamId);",
                    "CREATE INDEX IF NOT EXISTS idx_teammembers_userid ON TeamMembers(UserId);",
                    "CREATE INDEX IF NOT EXISTS idx_notifications_userid ON Notifications(UserId);",
                    "CREATE INDEX IF NOT EXISTS idx_notifications_isread ON Notifications(UserId, IsRead);"
                };

                foreach (var sql in indexes)
                {
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine("[DB] 인덱스 생성 완료!");
        }

        private static void AddInitialDataIfNeeded()
        {
            System.Diagnostics.Debug.WriteLine("[DB] 초기 데이터 확인 중...");
            
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();

                using (var checkCmd = new SQLiteCommand("SELECT COUNT(1) FROM Users;", conn))
                {
                    object result = null;
                    try { result = checkCmd.ExecuteScalar(); }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DB] Users 테이블 확인 실패: {ex.Message}");
                        result = null;
                    }

                    int userCount = result != null ? Convert.ToInt32(result) : 0;
                    System.Diagnostics.Debug.WriteLine($"[DB] 현재 사용자 수: {userCount}");

                    if (userCount == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("[DB] 초기 데이터 생성 시작!");
                        
                        // 비밀번호 해싱
                        string hashedPassword = AuthService.HashPassword("1234");
                        System.Diagnostics.Debug.WriteLine($"[DB] 해싱된 비밀번호: {hashedPassword.Substring(0, 10)}...");
                        
                        // 사용자 추가
                        string userSql = "INSERT INTO Users (Username, PasswordHash, Name, Role) VALUES (@u, @p, @n, @r); SELECT last_insert_rowid();";
                        int userId;
                        using (var cmd = new SQLiteCommand(userSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@u", "test");
                            cmd.Parameters.AddWithValue("@p", hashedPassword);
                            cmd.Parameters.AddWithValue("@n", "관리자_정훈");
                            cmd.Parameters.AddWithValue("@r", (int)UserRole.Admin);
                            userId = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[DB] 사용자 생성됨 - ID: {userId}");

                        // 과제 추가
                        string assignmentSql = "INSERT INTO Assignments (Course, Title, DueDate, Status, UserId) VALUES (@c, @t, @d, @s, @uid)";
                        using (var cmd = new SQLiteCommand(assignmentSql, conn))
                        {
                            // 과제 1
                            cmd.Parameters.AddWithValue("@c", "XR Lab");
                            cmd.Parameters.AddWithValue("@t", "포털 UI 프로토 (긴급)");
                            cmd.Parameters.AddWithValue("@d", DateTime.Now.AddHours(12).ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@s", "미제출");
                            cmd.Parameters.AddWithValue("@uid", userId);
                            cmd.ExecuteNonQuery();
                            System.Diagnostics.Debug.WriteLine($"[DB] 과제 1 추가됨 (UserId: {userId})");

                            // 과제 2
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@c", "Unity3D");
                            cmd.Parameters.AddWithValue("@t", "캐릭터 상호작용");
                            cmd.Parameters.AddWithValue("@d", DateTime.Now.AddDays(3).ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@s", "제출");
                            cmd.Parameters.AddWithValue("@uid", userId);
                            cmd.ExecuteNonQuery();
                            System.Diagnostics.Debug.WriteLine($"[DB] 과제 2 추가됨 (UserId: {userId})");
                            
                            // 과제 3 (추가)
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@c", "C# WinForms");
                            cmd.Parameters.AddWithValue("@t", "대시보드 UI 개선");
                            cmd.Parameters.AddWithValue("@d", DateTime.Now.AddDays(5).ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@s", "미제출");
                            cmd.Parameters.AddWithValue("@uid", userId);
                            cmd.ExecuteNonQuery();
                            System.Diagnostics.Debug.WriteLine($"[DB] 과제 3 추가됨 (UserId: {userId})");
                        }

                        // 게시글 추가
                        string postSql = "INSERT INTO Posts (Title, Content, AuthorId, CreatedDate) VALUES (@t, @c, @aid, @cd)";
                        using (var cmd = new SQLiteCommand(postSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@t", "환영합니다! 첫 게시글입니다.");
                            cmd.Parameters.AddWithValue("@c", "XBit 프로젝트 게시판 사용을 시작합니다.");
                            cmd.Parameters.AddWithValue("@aid", userId);
                            cmd.Parameters.AddWithValue("@cd", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.ExecuteNonQuery();
                        }
                        
                        System.Diagnostics.Debug.WriteLine("[DB] 초기 데이터 생성 완료!");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[DB] 이미 데이터가 있음. 초기화 스킵.");
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