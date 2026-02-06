using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PDTActivityTracker.Components.Model
{
    public class ActivityLog
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }

        public int? StoreId { get; set; }
        [ForeignKey("StoreId")]
        public Store? Store { get; set; }

        [Required]
        public string Action { get; set; } = default!; // "TaskCompleted", "PhotoUploaded", "Login", "StoreAssigned", etc.

        public string? EntityType { get; set; } // "ChecklistItem", "Store", "User"

        public int? EntityId { get; set; }

        public string? Details { get; set; } // JSON or description of the activity

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string? IPAddress { get; set; }
    }
}
