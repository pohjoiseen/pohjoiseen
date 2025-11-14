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
            o.UseSqlite($"Data Source={configuration["Holvi:DatabaseFile"]}"));
        // TODO: this should be optional for Fennica3
        services.AddScoped<IAmazonS3, AmazonS3Client>(provider => new AmazonS3Client(
            configuration["Holvi:S3:AccessKey"],
            // allow using secret manager for secret key
            configuration["Holvi:S3:SecretKey"],
            new AmazonS3Config
            {
                ForcePathStyle = false,
                RegionEndpoint = RegionEndpoint.USEast1,
                ServiceURL = configuration["Holvi:S3:Endpoint"]
            }
        ));
        services.AddScoped<PictureStorage>(provider => new PictureStorage(
            provider.GetService<IAmazonS3>()!,
            configuration["Holvi:S3:Bucket"]!,
            configuration["Holvi:S3:PublicUrl"]!
        ));
        services.AddScoped<PictureUpload>();
        return services;
    }
}