using Holvi.Models;
using Microsoft.EntityFrameworkCore;

namespace Holvi;

public class HolviDbContext(DbContextOptions<HolviDbContext> options) : DbContext(options)
{
    public DbSet<Picture> Pictures { get; init; } = null!;
    public DbSet<PictureSet> PictureSets { get; init; } = null!;
    public DbSet<Tag> Tags { get; init; } = null!;
    public DbSet<Article> Articles { get; init; } = null!;
    public DbSet<Post> Posts { get; init; } = null!;
    public DbSet<Book> Books { get; init; } = null!;
    public DbSet<Redirect> Redirects { get; init; } = null!;
    public DbSet<PendingPictureDeletion> PendingPictureDeletions { get; init; } = null!;
    public DbSet<Suggestion> Suggestions { get; init; } = null!;
    public DbSet<Revision> Revisions { get; init; } = null!;

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SetUpdatedAt();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        SetUpdatedAt();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Stamp UpdatedAt (UTC) on all added/modified entities that have it.  This used to be done with triggers,
    /// but those get silently dropped whenever EF rebuilds a SQLite table in a migration.
    /// </summary>
    private void SetUpdatedAt()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is EntityState.Added or EntityState.Modified &&
                entry.Metadata.FindProperty("UpdatedAt") is not null)
            {
                entry.Property("UpdatedAt").CurrentValue = now;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // manually specify some JSON mappings
        modelBuilder.Entity<Post>()
            .OwnsMany(p => p.CoatsOfArms, builder => builder.ToJson())
            .OwnsMany(p => p.Geo, builder =>
            {
                builder.ToJson();
                builder.OwnsMany(g => g.Links);
            });

        modelBuilder.Entity<Suggestion>(builder =>
        {
            // readable in the database
            builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(16);
            builder.HasIndex(s => new { s.TargetType, s.TargetId, s.Status });
            builder.HasIndex(s => s.Batch);
        });

        modelBuilder.Entity<Revision>().HasIndex(r => new { r.TargetType, r.TargetId });
    }
}