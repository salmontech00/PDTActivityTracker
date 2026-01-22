using System.ComponentModel.DataAnnotations;

namespace PDTActivityTracker.Components.Model
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = default!;

        [Required]
        public string Password { get; set; } = default!;

        [Required]
        public string Email { get; set; } = default!;

        [Required]
        public string Role { get; set; } = default!;

        [Required]
        public string Fullname { get; set; } = default!;

        public string? Position { get; set; }
        public string? Area { get; set; }
        public string? Tags { get; set; } // Comma-separated access tags

        public byte[]? ImageData { get; set; }
        public string? ImageMimeType { get; set; }
    }
}