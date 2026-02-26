using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskTrackingSystem.Models
{
    [Table("StatusTypes")]
    public class StatusType
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int Code { get; set; }
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;
    }
}
