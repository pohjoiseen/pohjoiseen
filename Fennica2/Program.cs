/*
 * Fennica2 entry point.
 * Usage: fennica2 serve|generate [--with-drafts] 
 */

using System.Text;
using Teos;
using Fennica2;
using Microsoft.Extensions.Localization;

// Locate content and build directories
// TODO: Make configurable?
string rootDir = Directory.GetCurrentDirectory();
string buildDir = rootDir + "/../Build";
Directory.CreateDirectory(buildDir);
string contentDir = rootDir + "/../Content";
if (!Directory.Exists(contentDir))
{
    throw new Exception("Content directory not found.  Build and Content must be subdirectories of the solution directory");
}

// Configure application
var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
{
    Args = args,
    WebRootPath = buildDir
});
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddRazorTemplating();
builder.Services.AddLogging(opt =>
{
    opt
        .AddConsole()
        .AddSimpleConsole(c =>
        {
            c.TimestampFormat = "[HH:mm:ss.fffff] ";
        });
});
var app = builder.Build();

// Configure Teos engine
var engine = new FennicaEngine("Fennica", contentDir, buildDir, args.Contains("--with-drafts"))
    .SetLogger(app.Logger)
    .AddStaticProcessor(new CopyFileProcessor("static"))
    .AddStaticProcessor(new ImageProcessor(new()
    {
        Sizes = new Dictionary<string, int>() { { "1x", FennicaProject.ImageSize }, { "2x", FennicaProject.ImageSize * 2 } }
    }))
    .AddContentType<Page>(".*\\.ru\\.article\\.md$", new MarkdownLoader(), new PageController())
    .AddContentType<Post>("([0-9]{4})-([0-9]{2})-([0-9]{2})-[^/]+\\.ru\\.post\\.md$", new MarkdownLoader(),
        new PostController())
    .AddContentType<Blog>("index\\.ru\\.blog\\.md$", new MarkdownLoader(),
        new BlogController(app.Services.GetService<IStringLocalizer<FennicaProject>>()!))
    .AddHTMLFormatter(new TypographyFormatter())
    .AddHTMLFormatter(new LinkAndImageFormatter(
        new List<string> { ".md" },
        new List<(string, string)> { ("1x", ""), ("2x", "2x") },
        true, true
    ))
    .AddHTMLFormatter(new GalleryFormatter())
    .AddHTMLFormatter(new MapFormatter())
    .AddHTMLFormatter(new HeadingIdFormatter());

if (args.Contains("dump-geo-texts"))
{
    if (args.Length != 2)
    {
        Console.WriteLine("Usage: fennica2 dump-geo-texts {language}");
    }

    string language = args[1];
    await engine.Prepare();
    app.Logger.LogInformation("Dumping titles and descriptions for all geo points in alphabetic order");
    
    var posts = (from p in engine.AllContent
        where p.Value.Item1 is Post post
              && post.Language == language
              orderby p.Value.Item1.Name
        select (Post)p.Value.Item1).ToList();
    Console.WriteLine("<!DOCTYPE HTML><html><head><meta charset=\"utf-8\"></head><body>");
    Console.OutputEncoding = Encoding.UTF8;
    foreach (var post in posts)
    {
        if (post.Geo != null)
        {
            foreach (var geo in post.Geo)
            {
                Console.WriteLine("<h1>" + geo.Title + "</h1>");
                Console.WriteLine("<p><i>" + geo.Subtitle + "</i></p>");
                Console.WriteLine("<p>" + geo.Description + "</p>");
                Console.WriteLine("<pre>" + post.Name + "</pre>");
                Console.WriteLine("<hr>");
            }
        }
    }
    Console.WriteLine("</body></html>");
    return 0;
}

// Serve mode
if (args.Contains("serve"))
{
    // Configure and run dev server
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
    }
    app.UseTeosServer(engine, "/ru/");
    app.UseStaticFiles();
    var appTask = app.RunAsync();

    // Load content and start watching
    await engine.Prepare();
    engine.Watch();

    // Wait for server to exit
    await appTask;
    return 0;
}
// Generate mode
else if (args.Contains("generate"))
{
    await engine.Prepare();
    return await engine.Generate() ? 0 : 1;
}
// Usage
else
{
    Console.WriteLine("This is Fennica2 website generator.");
    Console.WriteLine("Usage: fennica2 serve|generate [--with-drafts]");
    return 1;
}
