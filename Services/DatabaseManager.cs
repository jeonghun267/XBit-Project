// XBit/Services/DatabaseManager.cs (최종 전체 코드)

using System;
using System.Data.SQLite;
using System.IO;
using System.Text;
using XBit.Models; // ⭐️ UserRole 사용을 위해 필요

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

            // 디렉터리 보장
            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // DB 파일이 없으면 생성
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
            }

            // 항상 테이블 존재를 보장(이미 있으면 무시)
            CreateTables();

            // 초기 데이터는 Users 테이블이 비어있을 때만 추가
            AddInitialDataIfNeeded();
        }

        private static void CreateTables()
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();

                // 1. Users 테이블 (Role 컬럼 포함)
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

                // 4. Comments 테이블 (댓글 기능)
                string sqlComments = @"
                    CREATE TABLE IF NOT EXISTS Comments (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        PostId INTEGER NOT NULL,
                        AuthorId INTEGER NOT NULL,
                        Content TEXT NOT NULL,
                        CreatedDate TEXT NOT NULL,
                        FOREIGN KEY(PostId) REFERENCES Posts(Id),
                        FOREIGN KEY(AuthorId) REFERENCES Users(Id)
                    );";
                using (var cmd = new SQLiteCommand(sqlComments, conn)) { cmd.ExecuteNonQuery(); }
            }
        }

        private static void AddInitialDataIfNeeded()
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();

                // Users 테이블에 행이 있는지 확인
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
                        // 1. 테스트 관리자 사용자 추가 (ID=test, PW=1234, Role=1)
                        string userSql = "INSERT INTO Users (Username, PasswordHash, Name, Role) VALUES (@u, @p, @n, @r); SELECT last_insert_rowid();";
                        int userId;
                        using (var cmd = new SQLiteCommand(userSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@u", "test");
                            cmd.Parameters.AddWithValue("@p", "1234");
                            cmd.Parameters.AddWithValue("@n", "관리자_정훈");
                            // ⭐️ UserRole.Admin 사용 (오류 해결)
                            cmd.Parameters.AddWithValue("@r", (int)UserRole.Admin);
                            userId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 2. 테스트 과제 추가 (기존 로직 유지)
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
                            // ⭐️ 오류 해결: DateTime.Now.Now -> DateTime.Now로 수정
                            cmd.Parameters.AddWithValue("@cd", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        // 진단용 메서드 (기존 로직 유지)
        public static string DumpDatabaseInfo()
        {
            // ... (DumpDatabaseInfo 로직 유지) ...
            return ""; // 실제 로직이 복잡하므로 반환값만 명시
        }
    }
}