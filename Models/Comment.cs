// XBit/Models/Comment.cs (반응 카운트 호환성 추가: Dislikes 속성 포함)

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

        // 기존 호환성: 기존 코드가 사용하던 Likes 필드 유지
        public int Likes { get; set; }

        // 신규: 싫어요(Dislike) 카운트 표시용 속성
        public int Dislikes { get; set; }
    }
}