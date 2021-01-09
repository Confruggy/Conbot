using System;
using System.IO;

using Microsoft.EntityFrameworkCore;

namespace Conbot.ReminderPlugin
{
    public class ReminderContext : DbContext
    {
        public DbSet<Reminder> Reminders => Set<Reminder>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Reminder>()
                .Property(t => t.CreatedAt)
                .HasConversion(t => t, t => DateTime.SpecifyKind(t, DateTimeKind.Utc));
            modelBuilder.Entity<Reminder>()
                .Property(t => t.EndsAt)
                .HasConversion(t => t, t => DateTime.SpecifyKind(t, DateTimeKind.Utc));
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlite(
                    $"Data Source={Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location)!, "reminder")}.db")
                .UseLazyLoadingProxies();
        }
    }
}
