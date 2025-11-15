// XBit/Models/Task.cs

using System;

namespace XBit.Models
{
    public class ProjectTask
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Assignee { get; set; }
        public int Priority { get; set; }
        public string Status { get; set; }
        public int TeamId { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}