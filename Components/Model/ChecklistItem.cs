using System.ComponentModel.DataAnnotations;

namespace PDTActivityTracker.Components.Model
{
    public class ChecklistItem
    {
        public int Id { get; set; }

        public string? TaskName { get; set; }

        public string? Description { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public Category? Category { get; set; }

        public string? Section { get; set; }

        public bool IsPhotoRequired { get; set; } = false;

        public int NumberOfPhotos { get; set; } = 0;

        public bool IsMandatory { get; set; } = true;

        [Required]
        public string Frequency { get; set; } = "Daily";

        public bool IsRemarksRequired { get; set; } = false;

        public int? QuantityExpected { get; set; }

        public string? TaskType { get; set; } // "Regular", "Manpower", "PhotoOnly"

        public string? Position { get; set; }

        public string? SelectedStores { get; set; }

        public int SortOrder { get; set; } = 0;

        public bool IsDeleted { get; set; } = false;
    }

    public class ManpowerPosition
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; } = 1;
    }
}
