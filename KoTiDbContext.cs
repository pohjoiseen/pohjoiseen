using KoTi.Models;
using Microsoft.EntityFrameworkCore;

namespace KoTi;

public class KoTiDbContext : DbContext
{
    public DbSet<Country> Countries { get; set; }

    public DbSet<Region> Regions { get; set; }
    
    public DbSet<Area> Areas { get; set; }
    
    public DbSet<Place> Places { get; set; }
    
    public DbSet<Picture> Pictures { get; set; }

    public string DbPath { get; }

    public KoTiDbContext(DbContextOptions<KoTiDbContext> options) : base(options)
    {
    }
}