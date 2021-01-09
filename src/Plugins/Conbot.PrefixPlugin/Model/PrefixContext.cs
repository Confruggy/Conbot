using System.IO;

using Microsoft.EntityFrameworkCore;

namespace Conbot.PrefixPlugin
{
    public class PrefixContext : DbContext
    {
        public DbSet<Prefix> Prefixes => Set<Prefix>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Prefix>()
                .HasKey(x => new { x.GuildId, x.Text });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlite(
                    $"Data Source={Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location)!, "prefix")}.db")
                .UseLazyLoadingProxies();
        }
    }
}