using Holvi.Models;
using Microsoft.EntityFrameworkCore;

namespace Holvi;

public class HolviDbContext(DbContextOptions<HolviDbContext> options) : DbContext(options)
{
    public DbSet<Country> Countries { get; init; } = null!;
    public DbSet<Region> Regions { get; init; } = null!;
    public DbSet<Area> Areas { get; init; } = null!;
    public DbSet<Place> Places { get; init; } = null!;
    public DbSet<Picture> Pictures { get; init; } = null!;
    public DbSet<PictureSet> PictureSets { get; init; } = null!;
    public DbSet<Tag> Tags { get; init; } = null!;
    public DbSet<Article> Articles { get; init; } = null!;
    public DbSet<Post> Posts { get; init; } = null!;
    public DbSet<Redirect> Redirects { get; init; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // manually specify some JSON mappings
        modelBuilder.Entity<Post>()
            .OwnsMany<Post.CoatOfArms>(p => p.CoatsOfArms, builder => builder.ToJson())
            .OwnsMany<Post.GeoPoint>(p => p.Geo, builder => builder
                .OwnsMany<Post.Link>(g => g.Links, builder => builder.ToJson())
                .ToJson());
    }
}