using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskTrackingSystem.Models
{
    [Table("Users")]
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? ProfileImage { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;
        public string? PasswordHash { get; set; } = string.Empty;
        public string? PasswordResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
        public string Role { get; set; } = "Personel";
        public int FailedAttempts { get; set; } = 0;
        public bool MustChangePassword { get; set; } = true;
        public DateTime? LockoutEnd { get; set; }

        public DateTime CreatedTime { get; set; } = DateTime.Now;
    }

    public class CreateUserDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Role { get; set; }
        public string? ProfileImage { get; set; }
    }
}
