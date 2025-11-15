// XBit/Models/Project.cs (새 파일)
using System;
using XBit.Services; // AssignmentStatus enum 사용을 위해 필요

namespace XBit.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Course { get; set; }
        public string Title { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; } // 예: "미제출", "제출 완료"
        public string Content { get; set; }
    }
}