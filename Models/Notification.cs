// XBit/Models/Notification.cs

using System;

namespace XBit.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } // Task, Assignment, Team, System
        public bool IsRead { get; set; }
        public int? RelatedId { get; set; } // 관련된 작업/과제 ID
        public DateTime CreatedDate { get; set; }
    }
}