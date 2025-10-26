using Amazon;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Holvi;

public static class HolviExtensions
{
    public static IServiceCollection AddHolviServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<HolviDbContext>(o =>
            o.UseSqlite($"Data Source={Environment.GetEnvironmentVariable("KOTI__DBFILE")}"));
        services.AddScoped<IAmazonS3, AmazonS3Client>(provider => new AmazonS3Client(
            Environment.GetEnvironmentVariable("KOTI__DO_ACCESS_KEY"),
            // allow using secret manager for secret key
            Environment.GetEnvironmentVariable("KOTI__DO_SECRET_KEY") == null
                ? configuration["KOTI__DO_SECRET_KEY"] : Environment.GetEnvironmentVariable("KOTI__DO_SECRET_KEY"),
            new AmazonS3Config()
            {
                ForcePathStyle = false,
                RegionEndpoint = RegionEndpoint.USEast1,
                ServiceURL = Environment.GetEnvironmentVariable("KOTI__DO_ENDPOINT")
            }
        ));
        services.AddScoped<PictureStorage>(provider => new PictureStorage(
            provider.GetService<IAmazonS3>()!,
            Environment.GetEnvironmentVariable("KOTI__DO_BUCKET")!,
            Environment.GetEnvironmentVariable("KOTI__DO_BASE_PUBLIC_URL")!
        ));
        services.AddScoped<PictureUpload>();
        return services;
    }
}