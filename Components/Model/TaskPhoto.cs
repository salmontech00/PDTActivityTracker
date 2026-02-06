using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PDTActivityTracker.Components.Model
{
    public class TaskPhoto
    {
        [Key]
        public int Id { get; set; }

        public int? UploadedById { get; set; }
        [ForeignKey("UploadedById")]
        public User? UploadedBy { get; set; }

        public int? StoreId { get; set; }
        [ForeignKey("StoreId")]
        public Store? Store { get; set; }

        public int? ChecklistItemId { get; set; }
        [ForeignKey("ChecklistItemId")]
        public ChecklistItem? ChecklistItem { get; set; }

        public int? CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        public string? Description { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Image storage (same pattern as User.cs)
        public byte[]? ImageData { get; set; }
        public string? ImageMimeType { get; set; }
    }
}
