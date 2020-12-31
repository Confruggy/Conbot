using System;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace Conbot.TagPlugin
{
    public class TagContext : DbContext
    {
        public DbSet<Tag> Tags { get; set; }
        public DbSet<TagAlias> TagAliases { get; set; }
        public DbSet<TagModification> TagModifications { get; set; }
        public DbSet<TagUse> TagUses { get; set; }
        public DbSet<TagOwnerChange> TagOwnerChanges { get; set; }
        public DbSet<TagAliasOwnerChange> TagAliasOwnerChanges { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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
                .HasMany(t => t.OwnerChanges)
                .WithOne(t => t.Tag)
                .HasForeignKey(t => t.TagId);
            modelBuilder.Entity<Tag>()
                .HasAlternateKey(t => new { t.GuildId, t.Name });

            modelBuilder.Entity<TagAlias>()
                .HasMany(t => t.TagUses)
                .WithOne(t => t.UsedAlias)
                .HasForeignKey(t => t.UsedAliasId);
            modelBuilder.Entity<TagAlias>()
                .HasMany(t => t.OwnerChanges)
                .WithOne(t => t.TagAlias)
                .HasForeignKey(t => t.TagAliasId);
            modelBuilder.Entity<TagAlias>()
                .HasAlternateKey(t => new { t.GuildId, t.Name });

            modelBuilder.Entity<Tag>()
                .Property(t => t.CreatedAt)
                .HasConversion(t => t, t => DateTime.SpecifyKind(t, DateTimeKind.Utc));

            modelBuilder.Entity<TagAlias>()
                .Property(t => t.CreatedAt)
                .HasConversion(t => t, t => DateTime.SpecifyKind(t, DateTimeKind.Utc));

            modelBuilder.Entity<TagModification>()
                .Property(t => t.ModifiedAt)
                .HasConversion(t => t, t => DateTime.SpecifyKind(t, DateTimeKind.Utc));

            modelBuilder.Entity<TagUse>()
                .Property(t => t.UsedAt)
                .HasConversion(t => t, t => DateTime.SpecifyKind(t, DateTimeKind.Utc));
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlite(
                    $"Data Source={Path.Combine(Path.GetDirectoryName(this.GetType().Assembly.Location), "tag")}.db")
                .UseLazyLoadingProxies();
        }
    }
}
