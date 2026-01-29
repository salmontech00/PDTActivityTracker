using System.ComponentModel.DataAnnotations;

namespace PDTActivityTracker.Components.Model
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = default!;

        public string Icon { get; set; } = default!;

        public string ColorClass { get; set; } = default!;

        public int SortOrder { get; set; } = 0;
    }
}
