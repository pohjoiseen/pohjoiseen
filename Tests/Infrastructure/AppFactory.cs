using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Amazon.S3;
using Holvi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Infrastructure;

/// <summary>
/// Runs an app (Fennica3 or KoTi) in-process against its own copy of the test database and a fake picture storage.
/// </summary>
public abstract class AppFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
{
    public string DatabasePath { get; } = TestDatabase.CreateCopy();

    public FakePictureStore Store { get; } = new();

    protected AppFactory()
    {
        TestData.SeedStore(Store);
    }

    protected virtual IDictionary<string, string?> GetSettings() => new Dictionary<string, string?>
    {
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Warning",
        ["Holvi:DatabaseFile"] = DatabasePath,
        ["Holvi:S3:AccessKey"] = "test",
        ["Holvi:S3:SecretKey"] = "test",
        ["Holvi:S3:Endpoint"] = "http://127.0.0.1:9",
        ["Holvi:S3:Bucket"] = "test",
        ["Holvi:S3:PublicURL"] = FakePictureStore.PublicUrl,
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // not Development, so that we don't pick up *.appsettings.Development.json, and error handling is as in production
        builder.UseEnvironment("Testing");
        // build output (compressed static files in obj/) is used only in Development by default
        builder.UseStaticWebAssets();
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(GetSettings()));
        builder.ConfigureTestServices(services =>
        {
            services.AddScoped<IAmazonS3>(_ => Store.S3);
            services.AddScoped(_ => new PictureStorage(Store.S3, "test", FakePictureStore.PublicUrl,
                new HttpClient(Store.HttpHandler)));
        });
    }

    public HolviDbContext OpenDb() => TestDatabase.Open(DatabasePath);

    /// <summary>
    /// Project directory of the app.
    /// </summary>
    public string ContentRoot => Services.GetRequiredService<IWebHostEnvironment>().ContentRootPath;

    public HttpClient CreateNonRedirectingClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>
    /// Client making requests like htmx does (KoTi renders partials without layout then).
    /// </summary>
    public HttpClient CreateHtmxClient()
    {
        var client = CreateNonRedirectingClient();
        client.DefaultRequestHeaders.Add("HX-Request", "true");
        return client;
    }
}

public static class HttpClientExtensions
{
    private static readonly HtmlParser Parser = new();

    /// <summary>
    /// GET a page, check it's 200 and parse it.
    /// </summary>
    public static async Task<IHtmlDocument> GetDocumentAsync(this HttpClient client, string url)
    {
        var response = await client.GetAsync(url, TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.IsSuccessStatusCode, $"GET {url}: {(int)response.StatusCode}\n{html}");
        return Parser.ParseDocument(html);
    }

    public static IHtmlDocument ParseHtml(string html) => Parser.ParseDocument(html);
}
