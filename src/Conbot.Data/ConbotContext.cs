using Conbot.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Conbot.Data
{
    public class ConbotContext : DbContext
    {
        public DbSet<Tag> Tags { get; set; }
        public DbSet<TagAlias> TagAliases { get; set; }
        public DbSet<TagAliasCreation> TagAliasCreations { get; set; }
        public DbSet<TagCreation> TagCreations { get; set; }
        public DbSet<TagModification> TagModifications { get; set; }
        public DbSet<TagUse> TagUses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tag>()
                .HasOne(t => t.Creation)
                .WithOne(t => t.Tag)
                .HasForeignKey<TagCreation>(t => t.TagId);
            modelBuilder.Entity<Tag>()
                .HasMany(t => t.Aliases)
                .WithOne(t => t.Tag)
                .HasForeignKey(t => t.TagId);
            modelBuilder.Entity<Tag>()
                .HasMany(t => t.Modifications)
                .WithOne(t => t.Tag)
                .HasForeignKey(t => t.TagId);
            modelBuilder.Entity<Tag>()
                .HasMany(t => t.Uses)
                .WithOne(t => t.Tag)
                .HasForeignKey(t => t.TagId);
            modelBuilder.Entity<Tag>()
                .HasAlternateKey(t => new { t.GuildId, t.Name });

            modelBuilder.Entity<TagAlias>()
                .HasOne(t => t.Creation)
                .WithOne(t => t.TagAlias)
                .HasForeignKey<TagAliasCreation>(t => t.TagAliasId);
            modelBuilder.Entity<TagAlias>()
                .HasAlternateKey(t => new { t.GuildId, t.Name });
            modelBuilder.Entity<TagAlias>()
                .HasMany(t => t.TagUses)
                .WithOne(t => t.UsedAlias)
                .HasForeignKey(t => t.UsedAliasId);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlite("Data Source=database.db")
                .UseLazyLoadingProxies();
        }
    }
}
