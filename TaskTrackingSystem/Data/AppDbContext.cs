using Microsoft.EntityFrameworkCore;
using TaskTrackingSystem.Models;

namespace TaskTrackingSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<User> Users{ get; set; }
        public DbSet<TaskAssignment> TaskAssignments { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<StatusType> StatusTypes { get; set; }
        public DbSet<PriorityTypes> PriorityTypes { get; set; }
    }
}
