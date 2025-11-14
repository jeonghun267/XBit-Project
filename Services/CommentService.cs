// XBit/Services/CommentService.cs (오류 방지 및 안전한 DB 읽기 로직 적용)

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using XBit.Models;

namespace XBit.Services
{
    public class CommentService
    {
        // ⭐️ 1. 특정 게시물의 댓글 목록을 가져오는 메서드
        public List<Comment> GetCommentsByPostId(int postId)
        {
            var comments = new List<Comment>();
            if (postId <= 0) return comments;

            string sql = @"
                SELECT 
                    C.Id, C.PostId, C.AuthorId, U.Name, C.Content, C.CreatedDate, C.Likes 
                FROM Comments C
                LEFT JOIN Users U ON C.AuthorId = U.Id
                WHERE C.PostId = @pid
                ORDER BY C.CreatedDate ASC";

            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@pid", postId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        // ⭐️ 컬럼 인덱스를 변수에 저장하여 가독성을 높입니다.
                        int colId = 0, colPostId = 1, colAuthorId = 2, colAuthorName = 3, colContent = 4, colCreatedDate = 5, colLikes = 6;

                        while (reader.Read())
                        {
                            comments.Add(new Comment
                            {
                                Id = reader.GetInt32(colId),
                                PostId = reader.GetInt32(colPostId),
                                AuthorId = reader.GetInt32(colAuthorId),
                                // ⭐️ 안전한 Name 로딩 (NULL 체크)
                                AuthorName = reader.IsDBNull(colAuthorName) ? "탈퇴된 사용자" : reader.GetString(colAuthorName),
                                Content = reader.GetString(colContent),
                                CreatedDate = DateTime.Parse(reader.GetString(colCreatedDate)),
                                // ⭐️ Likes 로딩: NULL 체크 후 Int32로 변환 (오류 방지)
                                Likes = reader.IsDBNull(colLikes) ? 0 : reader.GetInt32(colLikes)
                            });
                        }
                    }
                }
            }
            return comments;
        }

        // ⭐️ 2. 댓글을 추가하는 메서드
        public bool AddComment(Comment comment)
        {
            if (AuthService.CurrentUser == null || string.IsNullOrWhiteSpace(comment.Content)) return false;

            comment.AuthorId = AuthService.CurrentUser.Id;

            // Likes 컬럼을 0으로 초기화하여 삽입
            string sql = "INSERT INTO Comments (PostId, AuthorId, Content, CreatedDate, Likes) VALUES (@pid, @aid, @content, @date, 0)";

            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@pid", comment.PostId);
                        cmd.Parameters.AddWithValue("@aid", comment.AuthorId);
                        cmd.Parameters.AddWithValue("@content", comment.Content);
                        cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
                catch (SQLiteException ex)
                {
                    // ⚠️ DB 오류 시 (예: Comments 테이블 없음) 디버그 메시지 확인
                    System.Diagnostics.Debug.WriteLine($"AddComment failed: {ex.Message}");
                    return false;
                }
            }
        }

        // ⭐️ 3. 댓글을 삭제하는 메서드
        public bool DeleteComment(int commentId)
        {
            if (AuthService.CurrentUser == null) return false;

            string checkSql = "SELECT AuthorId FROM Comments WHERE Id = @cid";
            int commentAuthorId = -1;

            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                conn.Open();
                using (var checkCmd = new SQLiteCommand(checkSql, conn))
                {
                    checkCmd.Parameters.AddWithValue("@cid", commentId);
                    object result = checkCmd.ExecuteScalar();
                    if (result != null)
                    {
                        commentAuthorId = Convert.ToInt32(result);
                    }
                }

                if (commentAuthorId != AuthService.CurrentUser.Id)
                {
                    return false;
                }

                string deleteSql = "DELETE FROM Comments WHERE Id = @cid AND AuthorId = @aid";
                using (var deleteCmd = new SQLiteCommand(deleteSql, conn))
                {
                    deleteCmd.Parameters.AddWithValue("@cid", commentId);
                    deleteCmd.Parameters.AddWithValue("@aid", AuthService.CurrentUser.Id);

                    return deleteCmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // ⭐️ 4. 댓글 공감 수 증가 메서드
        public bool IncrementLikes(int commentId)
        {
            string sql = "UPDATE Comments SET Likes = Likes + 1 WHERE Id = @cid";

            using (var conn = new SQLiteConnection(DatabaseManager.ConnectionString))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@cid", commentId);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
                catch (SQLiteException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"IncrementLikes failed: {ex.Message}");
                    return false;
                }
            }
        }
    }
}