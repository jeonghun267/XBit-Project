// XBit/Models/StatisticsData.cs

using System;

namespace XBit.Models
{
    public class StatisticsData
    {
        public int TotalAssignments { get; set; }
        public int CompletedAssignments { get; set; }
        public int OverdueAssignments { get; set; }
        public int UnreadNotifications { get; set; }
        public int TotalPosts { get; set; }
        public int MyPosts { get; set; }
        public int InProgressTasks { get; set; }
        public int[] MonthlyActivity { get; set; } // 최근 6개월 활동량
    }
}