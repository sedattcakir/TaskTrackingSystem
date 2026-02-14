using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TaskTrackingSystem.Models
{
    public class TaskItem
    {
        public Guid Id { get; set; } // public olacak ve çünkü her yerden çekilebilir olması gerekiyor.
        [MaxLength(100)]
        public string Title { get; set; } // Başlık yazılacak.
        [MaxLength(1000)]
        public string Description { get; set; } // Tanım yapılacak.
        public DateTime CreatedDate { get; set; } = DateTime.Now; // DataTime.Now işlemi şu anki tarihi başlangıç atar.
        public TaskStatusEnum Status { get; set; } = TaskStatusEnum.Bekliyor; // Burada da işlemi başlangıç seçimini bekliyor olarak açıyor.    

    }

}