using System;
using System.IO;

using Microsoft.EntityFrameworkCore;

namespace Conbot.WelcomePlugin
{
    public class WelcomeContext : DbContext
    {
        public DbSet<WelcomeConfiguration> Configurations => Set<WelcomeConfiguration>();

        public WelcomeContext() { }

        public WelcomeContext(DbContextOptions<WelcomeContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlite(
                    $"Data Source={Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location)!, "welcome")}.db")
                .UseLazyLoadingProxies()
                .EnableSensitiveDataLogging();
        }
    }
}
