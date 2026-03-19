using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PDTActivityTracker.Components.Model;

namespace PDTActivityTracker.Model
{
    public class AppDbContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public AppDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(_configuration.GetConnectionString("DatabaseConnectionString"));
        }

        public DbSet<User> Users { get; set; }
        public DbSet<ChecklistItem> ChecklistItems { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<StoreAssignmentLog> StoreAssignmentLogs { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<TaskPhoto> TaskPhotos { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Issue> Issues { get; set; }
        public DbSet<StoreDailyActivity> StoreDailyActivities { get; set; }
        public DbSet<TaskProgress> TaskProgresses { get; set; }
        public DbSet<AccessTag> AccessTags { get; set; }
    }
}