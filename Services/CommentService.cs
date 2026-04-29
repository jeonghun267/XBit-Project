// XBit/Services/CommentService.cs (좋아요/싫어요 반응 관리 + 레거시 IncrementLikes 호환 추가)

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using XBit.Models;

namespace XBit.Services
{
    public class CommentService
    {
        private readonly string _connectionString = DatabaseManager.ConnectionString;

        private const int LikeType = 1;
        private const int DislikeType = 0;

        // 게시물의 댓글 목록을 가져올 때 CommentLikes 기반의 좋아요/싫어요 카운트를 함께 읽도록 변경
        public List<Comment> GetCommentsByPostId(int postId)
        {
            var comments = new List<Comment>();
            if (postId <= 0) return comments;

            string sql = @"
                SELECT 
                    C.Id, C.PostId, C.AuthorId, U.Name, C.Content, C.CreatedDate,
                    -- CommentLikes 기반 카운트 (좋아요/싫어요)
                    IFNULL((SELECT COUNT(1) FROM CommentLikes CL WHERE CL.CommentId = C.Id AND CL.ReactionType = 1), 0) AS LikeCount,
                    IFNULL((SELECT COUNT(1) FROM CommentLikes CL WHERE CL.CommentId = C.Id AND CL.ReactionType = 0), 0) AS DislikeCount
                FROM Comments C
                LEFT JOIN Users U ON C.AuthorId = U.Id
                WHERE C.PostId = @pid
                ORDER BY C.CreatedDate ASC;";

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@pid", postId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        int colId = 0, colPostId = 1, colAuthorId = 2, colAuthorName = 3, colContent = 4, colCreatedDate = 5, colLikeCount = 6, colDislikeCount = 7;

                        while (reader.Read())
                        {
                            var created = reader.IsDBNull(colCreatedDate) ? DateTime.MinValue : DateTime.Parse(reader.GetString(colCreatedDate));
                            comments.Add(new Comment
                            {
                                Id = reader.GetInt32(colId),
                                PostId = reader.GetInt32(colPostId),
                                AuthorId = reader.GetInt32(colAuthorId),
                                AuthorName = reader.IsDBNull(colAuthorName) ? "탈퇴된 사용자" : reader.GetString(colAuthorName),
                                Content = reader.GetString(colContent),
                                CreatedDate = created,
                                Likes = reader.IsDBNull(colLikeCount) ? 0 : Convert.ToInt32(reader.GetValue(colLikeCount)),
                                Dislikes = reader.IsDBNull(colDislikeCount) ? 0 : Convert.ToInt32(reader.GetValue(colDislikeCount))
                            });
                        }
                    }
                }
            }
            return comments;
        }

        // 댓글 추가 (기존 동작 유지)
        public bool AddComment(Comment comment)
        {
            if (AuthService.CurrentUser == null || string.IsNullOrWhiteSpace(comment.Content)) return false;

            comment.AuthorId = AuthService.CurrentUser.Id;

            string sql = "INSERT INTO Comments (PostId, AuthorId, Content, CreatedDate, Likes) VALUES (@pid, @aid, @content, @date, 0)";

            using (var conn = new SQLiteConnection(_connectionString))
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
                    Debug.WriteLine($"AddComment failed: {ex.Message}");
                    return false;
                }
            }
        }

        // 댓글 삭제 (기존 동작 유지)
        public bool DeleteComment(int commentId)
        {
            if (AuthService.CurrentUser == null) return false;

            string checkSql = "SELECT AuthorId FROM Comments WHERE Id = @cid";
            int commentAuthorId = -1;

            using (var conn = new SQLiteConnection(_connectionString))
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

        // 신규: 댓글에 대한 반응 추가/토글(좋아요/싫어요)
        // reactionType: 1 = like, 0 = dislike
        public bool AddReaction(int commentId, int userId, int reactionType)
        {
            if (commentId <= 0 || userId <= 0) return false;
            if (reactionType != LikeType && reactionType != DislikeType) return false;

            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    // 기존 반응 조회
                    var selectSql = "SELECT ReactionType FROM CommentLikes WHERE CommentId = @cid AND UserId = @uid;";
                    using (var cmd = new SQLiteCommand(selectSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@cid", commentId);
                        cmd.Parameters.AddWithValue("@uid", userId);
                        var existing = cmd.ExecuteScalar();

                        if (existing != null)
                        {
                            int existingType = Convert.ToInt32(existing);
                            if (existingType == reactionType)
                            {
                                // 동일 반응이면 토글(삭제)
                                var delSql = "DELETE FROM CommentLikes WHERE CommentId = @cid AND UserId = @uid;";
                                using (var delCmd = new SQLiteCommand(delSql, conn))
                                {
                                    delCmd.Parameters.AddWithValue("@cid", commentId);
                                    delCmd.Parameters.AddWithValue("@uid", userId);
                                    delCmd.ExecuteNonQuery();
                                }
                                Debug.WriteLine($"[CommentService] Reaction removed: CommentId={commentId}, UserId={userId}");
                            }
                            else
                            {
                                // 다른 반응이면 업데이트
                                var updSql = "UPDATE CommentLikes SET ReactionType = @type, CreatedDate = @date WHERE CommentId = @cid AND UserId = @uid;";
                                using (var updCmd = new SQLiteCommand(updSql, conn))
                                {
                                    updCmd.Parameters.AddWithValue("@type", reactionType);
                                    updCmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                    updCmd.Parameters.AddWithValue("@cid", commentId);
                                    updCmd.Parameters.AddWithValue("@uid", userId);
                                    updCmd.ExecuteNonQuery();
                                }
                                Debug.WriteLine($"[CommentService] Reaction updated: CommentId={commentId}, UserId={userId}, Type={reactionType}");
                            }
                        }
                        else
                        {
                            // 새 반응 삽입
                            var insSql = "INSERT INTO CommentLikes (CommentId, UserId, ReactionType, CreatedDate) VALUES (@cid, @uid, @type, @date);";
                            using (var insCmd = new SQLiteCommand(insSql, conn))
                            {
                                insCmd.Parameters.AddWithValue("@cid", commentId);
                                insCmd.Parameters.AddWithValue("@uid", userId);
                                insCmd.Parameters.AddWithValue("@type", reactionType);
                                insCmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                insCmd.ExecuteNonQuery();
                            }
                            Debug.WriteLine($"[CommentService] Reaction added: CommentId={commentId}, UserId={userId}, Type={reactionType}");
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CommentService] AddReaction Error: {ex.Message}");
                return false;
            }
        }

        public int GetLikeCount(int commentId) => GetReactionCount(commentId, LikeType);
        public int GetDislikeCount(int commentId) => GetReactionCount(commentId, DislikeType);

        private int GetReactionCount(int commentId, int reactionType)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    var sql = "SELECT COUNT(1) FROM CommentLikes WHERE CommentId = @cid AND ReactionType = @type;";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@cid", commentId);
                        cmd.Parameters.AddWithValue("@type", reactionType);
                        var result = cmd.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CommentService] GetReactionCount Error: {ex.Message}");
                return 0;
            }
        }

        // 레거시 호환성 메서드: 기존 호출(IncrementLikes(commentId))을 지원합니다.
        // 가능하면 호출부를 AddReaction(...)으로 전환하세요.
        public bool IncrementLikes(int commentId)
        {
            if (commentId <= 0) return false;

            // 1) 로그인된 사용자가 있으면 CommentLikes 기반으로 좋아요 토글 수행
            var user = AuthService.CurrentUser;
            if (user != null)
            {
                return AddReaction(commentId, user.Id, LikeType);
            }

            // 2) 로그인 정보가 없으면 기존 Comments.Likes 컬럼을 직접 증가시켜 레거시 동작을 보장
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    var sql = "UPDATE Comments SET Likes = COALESCE(Likes,0) + 1 WHERE Id = @cid;";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@cid", commentId);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"IncrementLikes failed: {ex.Message}");
                return false;
            }
        }
    }
}