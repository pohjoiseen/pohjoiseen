using System.Diagnostics;
using Amazon;
using Amazon.S3;
using KoTi;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<KoTiDbContext>(o =>
    o.UseSqlite($"Data Source={Environment.GetEnvironmentVariable("KOTI__DBFILE")}"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IAmazonS3, AmazonS3Client>(provider => new AmazonS3Client(
    Environment.GetEnvironmentVariable("KOTI__DO_ACCESS_KEY"),
    // allow using secret manager for secret key
    Environment.GetEnvironmentVariable("KOTI__DO_SECRET_KEY") == null
        ? builder.Configuration["KOTI__DO_SECRET_KEY"] : Environment.GetEnvironmentVariable("KOTI__DO_SECRET_KEY"),
    new AmazonS3Config()
    {
        ForcePathStyle = false,
        RegionEndpoint = RegionEndpoint.USEast1,
        ServiceURL = Environment.GetEnvironmentVariable("KOTI__DO_ENDPOINT")
    }
));
builder.Services.AddScoped<PictureStorage>(provider => new PictureStorage(
    provider.GetService<IAmazonS3>()!,
    Environment.GetEnvironmentVariable("KOTI__DO_BUCKET")!,
    Environment.GetEnvironmentVariable("KOTI__DO_BASE_PUBLIC_URL")!
));

var app = builder.Build();

// migrations
if (args.Contains("migrate"))
{
    var context = app.Services.CreateScope().ServiceProvider.GetService<KoTiDbContext>();
    context!.Database.Migrate();
    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html"); ;

app.Run();
