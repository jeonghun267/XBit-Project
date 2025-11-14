// XBit/Services/BoardService.cs (전체 코드)

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using XBit.Models;

namespace XBit.Services
{
    public class BoardService
    {
        private Post MapReaderToPost(SQLiteDataReader reader)
        {
            return new Post
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Content = reader.GetString(2),
                AuthorId = reader.GetInt32(3),
                AuthorName = reader.GetString(4), // 조인된 작성자 이름
                CreatedDate = DateTime.Parse(reader.GetString(5))
            };
        }


        // 게시글 목록 조회 (작성자 이름 포함)
        public List<Post> GetAllPosts()
        {
            var posts = new List<Post>();
            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT 
                        P.Id, P.Title, P.Content, P.AuthorId, U.Name, P.CreatedDate 
                    FROM Posts P
                    LEFT JOIN Users U ON P.AuthorId = U.Id -- ⭐️ LEFT JOIN으로 수정
                    ORDER BY P.CreatedDate DESC";

                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        posts.Add(new Post
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            Content = reader.GetString(2),
                            AuthorId = reader.GetInt32(3),
                            AuthorName = reader.IsDBNull(4) ? "탈퇴된 사용자" : reader.GetString(4), // ⭐️ NULL 체크 추가
                            CreatedDate = DateTime.Parse(reader.GetString(5))
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
                    LEFT JOIN Users U ON P.AuthorId = U.Id -- ⭐️ LEFT JOIN으로 수정
                    WHERE P.Id = @pid";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@pid", postId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Post
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Content = reader.GetString(2),
                                AuthorId = reader.GetInt32(3),
                                AuthorName = reader.IsDBNull(4) ? "탈퇴된 사용자" : reader.GetString(4), // ⭐️ NULL 체크 추가
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