// XBit/Services/DatabaseManager.cs (CommentLikes ReactionType 추가 및 Tasks FK 보강, 인덱스 추가, DumpDatabaseInfo 구현)

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

            // 마이그레이션을 먼저 적용하여 누락된 컬럼을 보장한 뒤 인덱스를 생성합니다.
            ApplyMigrations();

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

                // 5. Tasks 테이블 (TeamId FK 추가)
                string sqlTasks = @"
                    CREATE TABLE IF NOT EXISTS Tasks (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        Assignee TEXT NOT NULL,
                        Priority INTEGER NOT NULL,
                        Status TEXT NOT NULL,
                        TeamId INTEGER NOT NULL,
                        CreatedDate TEXT NOT NULL,
                        FOREIGN KEY(TeamId) REFERENCES Teams(Id)
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

                // 9. CommentLikes 테이블 (반응 저장: ReactionType 포함)
                string sqlCreateCommentLikes = @"
                    CREATE TABLE IF NOT EXISTS CommentLikes (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        CommentId INTEGER NOT NULL,
                        UserId INTEGER NOT NULL,
                        ReactionType INTEGER NOT NULL DEFAULT 1, -- 1: 좋아요, 0: 싫어요 등
                        CreatedDate TEXT NOT NULL,
                        FOREIGN KEY(CommentId) REFERENCES Comments(Id),
                        FOREIGN KEY(UserId) REFERENCES Users(Id),
                        UNIQUE(CommentId, UserId)
                    );";
                using (var cmd = new SQLiteCommand(sqlCreateCommentLikes, conn)) { cmd.ExecuteNonQuery(); }
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
                    "CREATE INDEX IF NOT EXISTS idx_notifications_isread ON Notifications(UserId, IsRead);",
                    // CommentLikes 관련 인덱스
                    "CREATE INDEX IF NOT EXISTS idx_commentlikes_commentid ON CommentLikes(CommentId);",
                    "CREATE INDEX IF NOT EXISTS idx_commentlikes_userid ON CommentLikes(UserId);",
                    // ReactionType 컬럼이 없는 구 DB 환경 대비 조건부 생성
                    "CREATE INDEX IF NOT EXISTS idx_commentlikes_commentid_reaction ON CommentLikes(CommentId, ReactionType);"
                };

                foreach (var sql in indexes)
                {
                    try
                    {
                        // ReactionType 컬럼을 사용하는 인덱스는 컬럼 존재 여부를 확인
                        if (sql.Contains("idx_commentlikes_commentid_reaction"))
                        {
                            if (!TableHasColumn(conn, "CommentLikes", "ReactionType"))
                            {
                                System.Diagnostics.Debug.WriteLine("[DB] idx_commentlikes_commentid_reaction 스킵(ReactionType 컬럼 없음)");
                                continue;
                            }
                        }

                        using (var cmd = new SQLiteCommand(sql, conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        // 인덱스 생성 실패해도 전체 초기화가 중단되지 않도록 로깅 후 계속 진행
                        System.Diagnostics.Debug.WriteLine($"[DB] 인덱스 생성 중 오류(문장: {sql}): {ex.Message}");
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine("[DB] 인덱스 생성 완료!");
        }

        // 마이그레이션: ReactionType 컬럼 보장 등
        private static void ApplyMigrations()
        {
            System.Diagnostics.Debug.WriteLine("[DB] 마이그레이션 적용 시작...");

            try
            {
                using (var conn = new SQLiteConnection(ConnectionString))
                {
                    conn.Open();

                    // Assignments
                    EnsureColumnExists(conn, "Assignments", "SubmissionUrl TEXT");
                    EnsureColumnExists(conn, "Assignments", "SubmissionNote TEXT");

                    // Notifications
                    EnsureColumnExists(conn, "Notifications", "Severity TEXT DEFAULT 'Info'");
                    EnsureColumnExists(conn, "Notifications", "Icon TEXT");

                    // Comments 개선
                    EnsureColumnExists(conn, "Comments", "ParentCommentId INTEGER");
                    EnsureColumnExists(conn, "Comments", "IsEdited INTEGER DEFAULT 0");
                    EnsureColumnExists(conn, "Comments", "EditedDate TEXT");

                    // CommentLikes 테이블(기본 생성) 및 ReactionType 컬럼 보장
                    using (var cmd = new SQLiteCommand(conn))
                    {
                        string sqlCreateCommentLikes = @"
                            CREATE TABLE IF NOT EXISTS CommentLikes (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                CommentId INTEGER NOT NULL,
                                UserId INTEGER NOT NULL,
                                ReactionType INTEGER NOT NULL DEFAULT 1,
                                CreatedDate TEXT NOT NULL,
                                FOREIGN KEY(CommentId) REFERENCES Comments(Id),
                                FOREIGN KEY(UserId) REFERENCES Users(Id),
                                UNIQUE(CommentId, UserId)
                            );";
                        cmd.CommandText = sqlCreateCommentLikes;
                        cmd.ExecuteNonQuery();
                        System.Diagnostics.Debug.WriteLine("[DB] 테이블 확인/생성: CommentLikes");
                    }

                    // ReactionType 컬럼이 누락된 구 버전 대비 보장
                    EnsureColumnExists(conn, "CommentLikes", "ReactionType INTEGER NOT NULL DEFAULT 1");

                    // 추가 마이그레이션은 여기에...
                }

                System.Diagnostics.Debug.WriteLine("[DB] 마이그레이션 적용 완료!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB] 마이그레이션 중 오류: {ex.Message}");
            }
        }

        private static void EnsureColumnExists(SQLiteConnection conn, string tableName, string columnDefinition)
        {
            if (conn == null) return;
            try
            {
                var parts = columnDefinition.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) return;
                var columnName = parts[0];

                using (var cmd = new SQLiteCommand($"PRAGMA table_info([{tableName}]);", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var existing = reader["name"] as string;
                            if (!string.IsNullOrEmpty(existing) && string.Equals(existing, columnName, StringComparison.OrdinalIgnoreCase))
                            {
                                System.Diagnostics.Debug.WriteLine($"[DB] 컬럼 존재: {tableName}.{columnName}");
                                return;
                            }
                        }
                    }
                }

                string alterSql = $"ALTER TABLE [{tableName}] ADD COLUMN {columnDefinition};";
                using (var alterCmd = new SQLiteCommand(alterSql, conn))
                {
                    alterCmd.ExecuteNonQuery();
                }
                System.Diagnostics.Debug.WriteLine($"[DB] 컬럼 추가: {tableName}.{columnName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB] EnsureColumnExists 예외 ({tableName}): {ex.Message}");
            }
        }

        // 테이블에 특정 컬럼이 존재하는지 검사 (CreateIndexes에서 사용)
        private static bool TableHasColumn(SQLiteConnection conn, string tableName, string columnName)
        {
            if (conn == null) return false;
            try
            {
                using (var cmd = new SQLiteCommand($"PRAGMA table_info([{tableName}]);", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var existing = reader["name"] as string;
                            if (!string.IsNullOrEmpty(existing) && string.Equals(existing, columnName, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB] TableHasColumn 예외 ({tableName}.{columnName}): {ex.Message}");
            }
            return false;
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
                        
                        string hashedPassword = AuthService.HashPassword("1234");
                        System.Diagnostics.Debug.WriteLine($"[DB] 해싱된 비밀번호: {hashedPassword.Substring(0, 10)}...");
                        
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

                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@c", "C# WinForms");
                            cmd.Parameters.AddWithValue("@t", "대시보드 UI 개선");
                            cmd.Parameters.AddWithValue("@d", DateTime.Now.AddDays(5).ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@s", "미제출");
                            cmd.Parameters.AddWithValue("@uid", userId);
                            cmd.ExecuteNonQuery();
                        }

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

        // 간단한 DB 정보 덤프 (디버그용)
        public static string DumpDatabaseInfo()
        {
            var sb = new StringBuilder();
            try
            {
                using (var conn = new SQLiteConnection(ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT name, type FROM sqlite_master WHERE type IN ('table','view') AND name NOT LIKE 'sqlite_%' ORDER BY name;", conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var name = reader.GetString(0);
                                var type = reader.GetString(1);
                                int count = 0;
                                try
                                {
                                    using (var c = new SQLiteCommand($"SELECT COUNT(1) FROM [{name}];", conn))
                                    {
                                        var res = c.ExecuteScalar();
                                        count = res != null ? Convert.ToInt32(res) : 0;
                                    }
                                }
                                catch { /* ignore */ }
                                sb.AppendLine($"{type}: {name} (rows: {count})");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Dump error: {ex.Message}");
            }
            return sb.ToString();
        }
    }
}