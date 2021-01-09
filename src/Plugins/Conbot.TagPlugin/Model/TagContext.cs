using System;
using System.IO;

using Microsoft.EntityFrameworkCore;

namespace Conbot.TagPlugin
{
    public class TagContext : DbContext
    {
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<TagAlias> TagAliases => Set<TagAlias>();
        public DbSet<TagModification> TagModifications => Set<TagModification>();
        public DbSet<TagUse> TagUses => Set<TagUse>();
        public DbSet<TagOwnerChange> TagOwnerChanges => Set<TagOwnerChange>();
        public DbSet<TagAliasOwnerChange> TagAliasOwnerChanges => Set<TagAliasOwnerChange>();

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
                    $"Data Source={Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location)!, "tag")}.db")
                .UseLazyLoadingProxies();
        }
    }
}
