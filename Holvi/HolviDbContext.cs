using Holvi.Models;
using Microsoft.EntityFrameworkCore;

namespace Holvi;

public class HolviDbContext(DbContextOptions<HolviDbContext> options) : DbContext(options)
{
    public DbSet<Place> Places { get; init; } = null!;
    public DbSet<PlaceLocalization> PlaceLocalizations { get; init; } = null!;
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
            .OwnsMany(p => p.CoatsOfArms, builder => builder.ToJson())
            .OwnsMany(p => p.Geo, builder =>
            {
                builder.ToJson();
                builder.OwnsMany(g => g.Links);
            });
        modelBuilder.Entity<Place>().OwnsOne(p => p.Meta, builder => builder.ToJson());
        modelBuilder.Entity<PlaceLocalization>().OwnsOne(p => p.Meta, builder => builder.ToJson());
    }
}