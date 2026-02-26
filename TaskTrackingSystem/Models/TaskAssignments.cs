using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskTrackingSystem.Models
{
    [Table("TaskAssignments")]
    public class TaskAssignment
    {
        public int Id { get; set; }

        public Guid TaskId { get; set; }

        [ForeignKey("TaskId")]
        public TaskItem? Task { get; set; }

        public Guid UserId { get; set; }    

        [ForeignKey("UserId")]
        public User? User { get; set; }

        public DateTime CreatedTime { get; set; } = DateTime.Now;
    }
}