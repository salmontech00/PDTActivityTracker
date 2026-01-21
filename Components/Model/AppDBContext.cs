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
    }
}