// XBit/Services/BoardService.cs (최종 수정본 - 댓글 수 집계 및 이름 처리)

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using XBit.Models;

namespace XBit.Services
{
    public class BoardService
    {
        // ⚠️ DatabaseManager.ConnectionString이 static이 아니거나, 
        // BoardService가 인스턴스화되는 환경이라면 아래 코드는 수정이 필요합니다.
        // 현재 코드에서는 GetPostById에서 DatabaseManager.ConnectionString을 사용합니다.

        // Post.cs 모델에 CommentCount가 추가되었다고 가정합니다.

        // 게시글 목록 조회 (작성자 이름 포함 및 댓글 수 집계)
        public List<Post> GetAllPosts()
        {
            var posts = new List<Post>();

            // ⭐️ SQL 쿼리 수정: 
            // 1. LEFT JOIN Comments로 댓글 수를 집계 (COUNT(C.Id)).
            // 2. CASE WHEN으로 '관리자' 이름 처리.
            string sql = @"
                SELECT 
                    P.Id, P.Title, P.Content, P.AuthorId, P.CreatedDate,
                    U.Name AS OriginalName, 
                    COUNT(C.Id) AS CommentCount
                FROM Posts P
                LEFT JOIN Users U ON P.AuthorId = U.Id
                LEFT JOIN Comments C ON P.Id = C.PostId
                GROUP BY P.Id, P.Title, P.Content, P.AuthorId, P.CreatedDate, U.Name
                ORDER BY P.CreatedDate DESC";

            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string originalName = reader.IsDBNull(5) ? "탈퇴된 사용자" : reader.GetString(5);
                        string displayedName = originalName.Contains("관리자") ? "관리자" : originalName;

                        posts.Add(new Post
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            Content = reader.GetString(2),
                            AuthorId = reader.GetInt32(3),
                            AuthorName = displayedName, // ⭐️ '관리자'로 처리된 이름 사용
                            CreatedDate = DateTime.Parse(reader.GetString(4)),
                            CommentCount = reader.GetInt32(6) // ⭐️ 집계된 댓글 수
                        });
                    }
                }
            }
            return posts;
        }

        // 게시글 상세 조회
        public Post GetPostById(int postId)
        {
            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT P.Id, P.Title, P.Content, P.AuthorId, U.Name, P.CreatedDate
                    FROM Posts P
                    LEFT JOIN Users U ON P.AuthorId = U.Id
                    WHERE P.Id = @pid";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@pid", postId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string originalName = reader.IsDBNull(4) ? "탈퇴된 사용자" : reader.GetString(4);
                            string displayedName = originalName.Contains("관리자") ? "관리자" : originalName;

                            return new Post
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Content = reader.GetString(2),
                                AuthorId = reader.GetInt32(3),
                                AuthorName = displayedName, // ⭐️ '관리자'로 처리된 이름 사용
                                CreatedDate = DateTime.Parse(reader.GetString(5))
                            };
                        }
                    }
                }
            }
            return null;
        }


        // 게시글 생성
        public void CreatePost(Post post)
        {
            if (AuthService.CurrentUser == null) return;

            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                conn.Open();
                string sql = "INSERT INTO Posts (Title, Content, AuthorId, CreatedDate) VALUES (@t, @c, @aid, @cd)";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@t", post.Title);
                    cmd.Parameters.AddWithValue("@c", post.Content);
                    cmd.Parameters.AddWithValue("@aid", AuthService.CurrentUser.Id);
                    cmd.Parameters.AddWithValue("@cd", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 게시글 수정
        public void UpdatePost(Post post)
        {
            if (AuthService.CurrentUser == null || post.AuthorId != AuthService.CurrentUser.Id) return;

            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                conn.Open();
                string sql = "UPDATE Posts SET Title = @t, Content = @c WHERE Id = @pid";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@t", post.Title);
                    cmd.Parameters.AddWithValue("@c", post.Content);
                    cmd.Parameters.AddWithValue("@pid", post.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 게시글 삭제
        public bool DeletePost(int postId)
        {
            Post postToDelete = GetPostById(postId);

            if (postToDelete == null || postToDelete.AuthorId != AuthService.CurrentUser.Id)
            {
                return false;
            }

            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                try
                {
                    conn.Open();
                    string sql = "DELETE FROM Posts WHERE Id = @pid";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@pid", postId);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
                catch (SQLiteException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DeletePost failed: {ex.Message}");
                    return false;
                }
            }
        }
    }
}