// XBit/Models/Team.cs

using System;

namespace XBit.Models
{
    public class Team
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int OwnerId { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class TeamMember
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string Role { get; set; } // Owner, Leader, Member
        public DateTime JoinedDate { get; set; }
    }
}