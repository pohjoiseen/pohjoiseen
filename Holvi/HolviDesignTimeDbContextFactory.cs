using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Holvi;

/// <summary>
/// This is needed to run "dotnet ef database update" from this project directory.
/// </summary>
public class HolviDesignTimeDbContextFactory : IDesignTimeDbContextFactory<HolviDbContext>
{
    public HolviDbContext CreateDbContext(string[] args)
    {
        if (args.Length == 0)
        {
            throw new Exception("Database file argument required");
        }
        var optionsBuilder = new DbContextOptionsBuilder<HolviDbContext>();
        optionsBuilder.UseSqlite($"Data Source={args[0]}");
        return new HolviDbContext(optionsBuilder.Options);
    }
}