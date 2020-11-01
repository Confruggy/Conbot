using System.IO;
using Microsoft.EntityFrameworkCore;

namespace Conbot.PrefixPlugin
{
    public class PrefixContext : DbContext
    {
        public DbSet<Prefix> Prefixes { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Prefix>()
                .HasKey(x => new { x.GuildId, x.Text });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlite(
                    $"Data Source={Path.Combine(Path.GetDirectoryName(this.GetType().Assembly.Location), "prefix")}.db")
                .UseLazyLoadingProxies();
        }
    }
}