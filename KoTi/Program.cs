using Fennica3;
using Holvi;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("KoTi.appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"KoTi.appsettings.{builder.Environment.EnvironmentName}.json", optional: true);
builder.Services.AddHolviServices(builder.Configuration);
builder.Services.AddFennicaServices(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// KoTi fully includes Fennica3 (for post previews)
var f3Assembly = typeof(Fennica3.Fennica3).Assembly;
var part = new AssemblyPart(f3Assembly);
builder.Services.AddControllersWithViews().ConfigureApplicationPartManager(apm => apm.ApplicationParts.Add(part));

var app = builder.Build();

if (args.Contains("--help"))
{
    Console.WriteLine("This is Fennica3/KoTi backoffice.");
    Console.WriteLine("Special commands:");
    Console.WriteLine("  migrate                      Apply all database migrations.");
    Console.WriteLine("Otherwise this works as a regular ASP.NET Core Web app.");
    return;
}

// various script-like stuff
if (args.Length > 0)
{
    switch (args[0])
    {
        case "migrate":
        {
            var context = app.Services.CreateScope().ServiceProvider.GetService<HolviDbContext>();
            context!.Database.Migrate();
            return;
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new CompositeFileProvider(
        app.Environment.WebRootFileProvider,
        new PhysicalFileProvider(Path.GetFullPath("../Fennica3/wwwroot")))
});
app.UseRouting();
app.UseRequestLocalization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html"); ;

app.Run();
