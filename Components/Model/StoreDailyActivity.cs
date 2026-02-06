using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PDTActivityTracker.Components.Model
{
    public class StoreDailyActivity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StoreId { get; set; }

        [ForeignKey("StoreId")]
        public Store? Store { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public int CurrentCategoryId { get; set; }

        [ForeignKey("CurrentCategoryId")]
        public Category? CurrentCategory { get; set; }

        public bool IsCompleted { get; set; } = false;

        public DateTime? CompletedAt { get; set; }

        public int StartedById { get; set; }

        [ForeignKey("StartedById")]
        public User? StartedBy { get; set; }
    }
}
