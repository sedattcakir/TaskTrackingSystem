using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static TaskTrackingSystem.Models.StatusEnum;

namespace TaskTrackingSystem.Models
{
    [Table("Tasks")]
    public class TaskItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public Project? Project { get; set; }

        public int StatusCode { get; set; } = 0;

        [NotMapped]
        public TaskStatusEnum Status
        {
            get => (StatusEnum.TaskStatusEnum)StatusCode;
            set => StatusCode = (int)value;
        }
        [Column("Priority")]
        public int PriorityCode { get; set; }

        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
        [Column("CompletionDate")]          
        public DateTime? CompletionDate { get; set; }

        public DateTime? StartDate { get; set; }

        [Column("CreatedTime")]
        public DateTime CreatedTime { get; set; } = DateTime.Now;
        public List<TaskAssignment> TaskAssignments { get; set; } = new();
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class CreateTaskDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid ProjectId { get; set; }
        public int Priority { get; set; } 
        public List<Guid> UserIds { get; set; } = new();
        public DateTime? CompletionDate { get; set; }
        public DateTime? StartDate { get; set; }
    }

    public class UpdateTaskDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public int Priority { get; set; }
        public Guid ProjectId { get; set; }
        public List<Guid> UserIds { get; set; } = new();
        public DateTime? CompletionDate { get; set; }
        public DateTime? StartDate { get; set; }
    }

}