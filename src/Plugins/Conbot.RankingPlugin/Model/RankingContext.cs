using System.IO;
using Microsoft.EntityFrameworkCore;

namespace Conbot.RankingPlugin
{
    public class RankingContext : DbContext
    {
        public DbSet<Rank> Ranks { get; set; }
        public DbSet<RankGuildConfiguration> GuildConfigurations { get; set; }
        public DbSet<RankRoleReward> RoleRewards { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Rank>()
                .HasAlternateKey(r => new { r.GuildId, r.UserId });

            modelBuilder.Entity<RankGuildConfiguration>()
                .HasMany(r => r.RoleRewards)
                .WithOne(r => r.GuildConfiguration)
                .HasForeignKey(r => r.GuildId);

            modelBuilder.Entity<RankRoleReward>()
                .HasAlternateKey(r => new { r.GuildId, r.RoleId });

            modelBuilder.Entity<RankRoleReward>()
                .HasAlternateKey(r => new { r.GuildId, r.Level });

            modelBuilder.Entity<RankRoleReward>()
                .HasAlternateKey(r => new { r.Level, r.RoleId });

            modelBuilder.Entity<RankRoleReward>()
                .HasAlternateKey(r => new { r.GuildId, r.Level, r.RoleId });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlite(
                    $"Data Source={Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "Ranking")}.db")
                .UseLazyLoadingProxies();
        }
    }
}
