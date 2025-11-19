using System;
using System.Collections.Generic;
using System.Linq;
using XBit.Models;

namespace XBit.Services
{
    public class StatisticsService
    {
        private readonly AssignmentService _assignmentService = new AssignmentService();
        private readonly NotificationService _notificationService = new NotificationService();
        private readonly BoardService _boardService = new BoardService();
        private readonly TaskService _taskService = new TaskService();

        public StatisticsData GetUserStatistics(int userId)
        {
            var stats = new StatisticsData();

            var assignments = _assignmentService.GetAssignmentsForUser(userId) ?? new List<Assignment>();
            stats.TotalAssignments = assignments.Count;
            stats.CompletedAssignments = assignments.Count(a => StatusHelper.IsCompleted(a.Status));
            stats.OverdueAssignments = assignments.Count(a => a.DueDate < DateTime.Now && !StatusHelper.IsCompleted(a.Status));

            stats.UnreadNotifications = SafeGetUnreadNotifications(userId);
            var posts = SafeGetAllPosts();
            stats.TotalPosts = posts.Count;
            stats.MyPosts = posts.Count(p => p.AuthorId == userId);

            stats.InProgressTasks = SafeCountInProgressTasksForUser(userId);

            // 최근 6개월 활동(완료된 과제 + 생성된 게시물)
            var months = new int[6];
            try
            {
                var now = DateTime.Now;
                for (int i = 0; i < 6; i++)
                {
                    var start = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                    var end = start.AddMonths(1);
                    int count = 0;
                    count += assignments.Count(a => a.DueDate >= start && a.DueDate < end && StatusHelper.IsCompleted(a.Status));
                    count += posts.Count(p => p.CreatedDate >= start && p.CreatedDate < end);
                    months[5 - i] = count;
                }
            }
            catch
            {
                months = new int[6];
            }
            stats.MonthlyActivity = months;

            return stats;
        }
            
        private int SafeGetUnreadNotifications(int userId)
        {
            try { return _notificationService.GetUnreadCount(userId); }
            catch { return 0; }
        }

        private List<Post> SafeGetAllPosts()
        {
            try { return _boardService.GetAllPosts() ?? new List<Post>(); }
            catch { return new List<Post>(); }
        }

        private int SafeCountInProgressTasksForUser(int userId)
        {
            try
            {
                int inProgress = 0;
                var teamService = new TeamService();
                var userTeams = teamService.GetTeamsByUser(userId) ?? new List<Team>();
                foreach (var team in userTeams)
                {
                    try
                    {
                        var tasks = _taskService.GetTasksByTeam(team.Id) ?? new List<ProjectTask>();
                        inProgress += tasks.Count(t => StatusHelper.IsInProgress(t.Status));
                    }
                    catch
                    {
                        // 내부 팀 작업 집계 중 오류 발생 시 무시하고 다음 팀으로 진행
                    }
                }
                return inProgress;
            }
            catch
            {
                return 0;
            }
        }
    }
}