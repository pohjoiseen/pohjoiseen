using KoTi;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddKoTiServices(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (args.Contains("--help"))
{
    Console.WriteLine("This is Fennica3/KoTi backoffice.");
    Console.WriteLine("Special commands:");
    Console.WriteLine("  migrate                      Apply all database migrations.");
    Console.WriteLine("  import-fennica2-posts <dir>  Import all Fennica2 format blog posts.");
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
            var context = app.Services.CreateScope().ServiceProvider.GetService<KoTiDbContext>();
            context!.Database.Migrate();
            return;
        }

        case "import-fennica2-posts":
        {
            if (args.Length < 2)
            {
                throw new Exception("Input directory required!");
            }
            await ImportFennica2Posts.Do(args[1], app.Logger,
                app.Services.CreateScope().ServiceProvider.GetService<KoTiDbContext>()!,
                app.Services.CreateScope().ServiceProvider.GetService<PictureUpload>()!);
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

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html"); ;

app.Run();
