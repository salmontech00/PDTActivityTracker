using System.ComponentModel.DataAnnotations;

namespace PDTActivityTracker.Components.Model
{
    public class ChecklistItem
    {
        public int Id { get; set; }

        public string? TaskName { get; set; }

        public string? Description { get; set; }

        [Required]
        public string Category { get; set; } = default!;

        public string? Section { get; set; }

        public bool IsPhotoRequired { get; set; } = false;

        public int NumberOfPhotos { get; set; } = 0;

        public bool IsMandatory { get; set; } = true;

        [Required]
        public string Frequency { get; set; } = "Daily";

        public bool IsRemarksRequired { get; set; } = false;

        public int SortOrder { get; set; } = 0;
    }
}
