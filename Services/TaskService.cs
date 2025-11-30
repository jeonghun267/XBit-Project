using System;
using System.Collections.Generic;
using XBit.Models;

namespace XBit.Services
{
    public class TaskService
    {
        private readonly TaskRepository _repository;

        public TaskService() : this(new TaskRepository()) { }

        // 생성자 주입 허용
        public TaskService(TaskRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public List<ProjectTask> GetTasksByTeam(int teamId)
        {
            if (teamId <= 0) return new List<ProjectTask>();
            return _repository.GetTasksByTeam(teamId);
        }

        public bool UpdateTaskStatus(int taskId, string newStatus)
        {
            if (taskId <= 0) return false;
            if (string.IsNullOrWhiteSpace(newStatus)) return false;

            return _repository.UpdateTaskStatus(taskId, newStatus);
        }

        public bool AddTask(string title, string assignee, int priority, string status, int teamId)
        {
            if (string.IsNullOrWhiteSpace(title)) return false;
            if (string.IsNullOrWhiteSpace(assignee)) assignee = "Unassigned";
            if (priority < 0) priority = 0;
            if (string.IsNullOrWhiteSpace(status)) status = "New";
            if (teamId <= 0) return false;

            return _repository.AddTask(title, assignee, priority, status, teamId);
        }
    }
}