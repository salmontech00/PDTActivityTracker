using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PDTActivityTracker.Components.Model
{
    public class StoreAssignmentLog
    {
        [Key]
        public int Id { get; set; }

        public int StoreId { get; set; }
        [ForeignKey("StoreId")]
        public Store? Store { get; set; }

        public int? UserId { get; set; } // Nullable if we want to track unassignment without a user, strictly speaking usually we track who was assigned/unassigned
        [ForeignKey("UserId")]
        public User? User { get; set; }

        public string Role { get; set; } = default!; // "Store Head" or "Area Head"
        public string Action { get; set; } = default!; // "Assigned", "Removed", "Replaced"

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string? Remarks { get; set; }
    }
}
