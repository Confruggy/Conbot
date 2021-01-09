using System.IO;

using Microsoft.EntityFrameworkCore;

namespace Conbot.TimeZonePlugin
{
    public class TimeZoneContext : DbContext
    {
        public DbSet<UserTimeZone> UserTimeZones => Set<UserTimeZone>();
        public DbSet<GuildTimeZone> GuildTimeZones => Set<GuildTimeZone>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlite(
                    $"Data Source={Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location)!, "timezone")}.db")
                .UseLazyLoadingProxies();
        }
    }
}
