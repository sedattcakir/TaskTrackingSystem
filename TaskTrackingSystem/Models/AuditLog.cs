namespace TaskTrackingSystem.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string? UserEmail { get; set; } = string.Empty;
        public string? Action { get; set; } = string.Empty;
        public string? IpAddress { get; set; } 
        public DateTime? CreatedTime { get; set; } = DateTime.UtcNow;

    }

}