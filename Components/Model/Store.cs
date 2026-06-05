using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PDTActivityTracker.Components.Model
{
    public class Store
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string StoreCode { get; set; } = default!;

        [Required]
        public string StoreName { get; set; } = default!;

        [Required]
        public string Location { get; set; } = default!; // Region/City

        public string Status { get; set; } = "Active";

        public double? GPSLatitude { get; set; }
        public double? GPSLongitude { get; set; }

        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }

        public bool IsDeleted { get; set; } = false;

        // Assignments
        public int? StoreHeadId { get; set; }
        [ForeignKey("StoreHeadId")]
        public User? StoreHead { get; set; }

        public int? AreaHeadId { get; set; }
        [ForeignKey("AreaHeadId")]
        public User? AreaHead { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is Store other)
            {
                return Id == other.Id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
