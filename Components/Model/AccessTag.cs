using System.ComponentModel.DataAnnotations;

namespace PDTActivityTracker.Components.Model
{
    public class AccessTag
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = default!;

        public string? Description { get; set; }
    }
}
