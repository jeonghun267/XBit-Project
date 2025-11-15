// Models/Post.cs

using System;

namespace XBit.Models
{
    public class Post
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public int AuthorId { get; set; }
        public string AuthorName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Views { get; set; }

        // ⭐️ 추가: 댓글 수를 저장할 필드
        public int CommentCount { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}