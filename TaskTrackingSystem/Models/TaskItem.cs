using System.Data;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TaskTrackingSystem.Models
{

    public enum TaskStatusEnum
    {
        Bekliyor = 0,
        DevamEdiyor = 1,
        Tamamlandi = 2,
    }
    public class TaskItem
    {
        public Guid Id { get; set; } // public olacak ve çünkü her yerden çekilebilir olması gerekiyor.

        [Required(ErrorMessage = "Bu alan boş olmaması gerekiyor. Başlık yazılması gerekiyor.")]
        [MaxLength(100)]
        public string Title { get; set; } // Başlık yazılacak.

        [Required(ErrorMessage = "Bu alan boş olmaması lazım. Tanım yapılması gerekiyor.")]
        [MaxLength(1000)]
        public string Description { get; set; } // Tanım yapılacak.
        public DateTime CreatedDate { get; set; } = DateTime.Now; // DataTime.Now işlemi şu anki tarihi başlangıç atar.
        public TaskStatusEnum Status { get; set; } = TaskStatusEnum.Bekliyor; // Burada da işlemi başlangıç seçimini bekliyor olarak açıyor.    

    }

    public class CreateTaskDto
    {
        [Required(ErrorMessage = "Bu alan boş olmaması gerekiyor. Başlık yazılması gerekiyor.")]
        [MaxLength(100)]
        public string Title { get; set; } // Başlık yazılacak.

        [Required(ErrorMessage = "Bu alan boş olmaması lazım. Tanım yapılması gerekiyor.")]
        [MaxLength(1000)]
        public string Description { get; set; } // Tanım yapılacak.
    }

    public class UpdateTaskDto // güncelleme olduğu için anlık durumu takip edeceğimiz için status buna dahil olacak diğerleri sabit kalacak.
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public TaskStatusEnum Status { get; set; }
    }

    public class UpdateFullTaskDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int Status { get; set; }
    }

}