// XBit/Models/Post.cs (새 파일 생성)
using System;

namespace XBit.Models
{
    public class Post
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public int AuthorId { get; set; }
        public string AuthorName { get; set; } // 조인하여 표시할 이름
        public DateTime CreatedDate { get; set; }
    }
}