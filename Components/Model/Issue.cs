using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PDTActivityTracker.Components.Model
{
    public class Issue
    {
        [Key]
        public int Id { get; set; }

        public int StoreId { get; set; }
        [ForeignKey("StoreId")]
        public Store? Store { get; set; }

        [Required]
        public string Title { get; set; } = default!;

        public string? Description { get; set; }

        public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical

        public string Status { get; set; } = "Open"; // Open, Resolved

        public int? ReportedById { get; set; }
        [ForeignKey("ReportedById")]
        public User? ReportedBy { get; set; }

        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ResolvedAt { get; set; }
        public byte[]? ImageData { get; set; }
        public string? ImageMimeType { get; set; }

        public string? AreaHeadRemark { get; set; }
        public int? ReviewedById { get; set; }
        [ForeignKey("ReviewedById")]
        public User? ReviewedBy { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTime? CaptureTime { get; set; }
    }
}
