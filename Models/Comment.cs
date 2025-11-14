// XBit/Models/Comment.cs (최종 수정본 - Likes 속성 추가)

using System;

namespace XBit.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public int PostId { get; set; }        // 댓글이 달린 게시물 ID
        public int AuthorId { get; set; }      // 댓글 작성자 ID
        public string AuthorName { get; set; } // 댓글 작성자 이름 (조회 시 Users와 JOIN)
        public string Content { get; set; }    // 댓글 내용
        public DateTime CreatedDate { get; set; } // 작성 시간

        // ⭐️ 오류 해결의 핵심: Likes 속성 추가
        public int Likes { get; set; }
    }
}