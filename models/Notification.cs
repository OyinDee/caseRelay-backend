using System.ComponentModel.DataAnnotations;

namespace CaseRelayAPI.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "case", "admin", "system"
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? RelatedCaseId { get; set; }
        public string? ActionBy { get; set; }
    }
}
