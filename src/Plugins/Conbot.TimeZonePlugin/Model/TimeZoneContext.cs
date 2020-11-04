using System.IO;
using Microsoft.EntityFrameworkCore;

namespace Conbot.TimeZonePlugin
{
    public class TimeZoneContext : DbContext
    {
        public DbSet<UserTimeZone> UserTimeZones { get; set; }
        public DbSet<GuildTimeZone> GuildTimeZones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlite(
                    $"Data Source={Path.Combine(Path.GetDirectoryName(this.GetType().Assembly.Location), "timezone")}.db")
                .UseLazyLoadingProxies();
        }
    }
}
