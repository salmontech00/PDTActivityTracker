using System.ComponentModel.DataAnnotations;

namespace PDTActivityTracker.Components.Model
{
    public class Area
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = default!;

        [Required]
        public string Type { get; set; } = default!; // e.g., Municipality, Barangay, or Store/Region if applicable
    }
}
