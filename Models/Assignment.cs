// XBit/Models/Assignment.cs (전체 코드)

using System;

namespace XBit.Models
{
    public class Assignment
    {
        public int Id { get; set; }
        public string Course { get; set; }
        public string Title { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; }
    }
}