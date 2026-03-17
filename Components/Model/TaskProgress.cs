using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PDTActivityTracker.Components.Model
{
    public class TaskProgress
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StoreDailyActivityId { get; set; }

        [ForeignKey("StoreDailyActivityId")]
        public StoreDailyActivity? StoreDailyActivity { get; set; }

        [Required]
        public int ChecklistItemId { get; set; }

        [ForeignKey("ChecklistItemId")]
        public ChecklistItem? ChecklistItem { get; set; }

        public bool IsCompleted { get; set; } = false;

        public int? ActualQuantity { get; set; }

        public string? PositionActuals { get; set; }

        public string? PositionExpected { get; set; }

        public string? Remarks { get; set; }

        public DateTime? CompletedAt { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
