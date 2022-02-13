using System.IO;

using Microsoft.EntityFrameworkCore;

namespace Conbot.ModerationPlugin;

public class ModerationContext : DbContext
{
    public DbSet<ModerationGuildConfiguration> GuildConfigurations => Set<ModerationGuildConfiguration>();
    public DbSet<TemporaryMutedUser> TemporaryMutedUsers => Set<TemporaryMutedUser>();
    public DbSet<PreconfiguredMutedRole> PreconfiguredMutedRoles => Set<PreconfiguredMutedRole>();

    public ModerationContext(DbContextOptions<ModerationContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TemporaryMutedUser>()
            .HasOne(t => t.GuildConfiguration)
            .WithMany(t => t.TemporaryMutedUsers)
            .HasForeignKey(t => t.GuildId);
        modelBuilder.Entity<TemporaryMutedUser>()
            .HasAlternateKey(t => new { t.GuildId, t.UserId });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSqlite(
                $"Data Source={Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location)!, "moderation")}.db")
            .UseLazyLoadingProxies()
            .EnableSensitiveDataLogging();
    }
}