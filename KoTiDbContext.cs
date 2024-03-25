using KoTi.Models;
using Microsoft.EntityFrameworkCore;

namespace KoTi;

public class KoTiDbContext(DbContextOptions<KoTiDbContext> options) : DbContext(options)
{
    public DbSet<Country> Countries { get; init; } = null!;
    public DbSet<Region> Regions { get; init; } = null!;
    public DbSet<Area> Areas { get; init; } = null!;
    public DbSet<Place> Places { get; init; } = null!;
    public DbSet<Picture> Pictures { get; init; } = null!;
    public DbSet<PictureSet> PictureSets { get; init; } = null!;
    public DbSet<Tag> Tags { get; init; } = null!;
}