using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

[assembly: AssemblyFixture(typeof(Tests.Infrastructure.BrowserFixture))]

namespace Tests.Infrastructure;

/// <summary>
/// One headless Chromium for all browser tests.  Uses system Chromium (or CHROMIUM_PATH), or the one installed
/// by Playwright if there is none; browser tests are skipped if neither is available.
/// </summary>
public class BrowserFixture : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string? _error;

    public async ValueTask InitializeAsync()
    {
        try
        {
            _playwright = await Playwright.CreateAsync();
            var executablePath = Environment.GetEnvironmentVariable("CHROMIUM_PATH") ??
                                 new[] { "/usr/bin/chromium", "/usr/bin/chromium-browser" }.FirstOrDefault(File.Exists);
            _browser = await _playwright.Chromium.LaunchAsync(new() { ExecutablePath = executablePath });
        }
        catch (PlaywrightException e)
        {
            _error = e.Message;
        }
    }

    public IBrowser Browser
    {
        get
        {
            Assert.SkipWhen(_browser is null, $"No browser for browser tests: {_error}");
            return _browser!;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }
        _playwright?.Dispose();
    }
}

/// <summary>
/// Page in a fresh browser context, with all external requests blocked (fonts, CDNs, map tiles, analytics),
/// pictures served from fake storage, and JS errors collected.
/// </summary>
public class TestPage : IAsyncDisposable
{
    private readonly IBrowserContext _context;
    public IPage Page { get; }
    public string BaseUrl { get; }
    public List<string> Errors { get; } = [];

    private TestPage(IBrowserContext context, IPage page, string baseUrl)
    {
        _context = context;
        Page = page;
        BaseUrl = baseUrl;
    }

    public static async Task<TestPage> CreateAsync<T>(BrowserFixture browser, LiveAppFactory<T> factory) where T : class
    {
        var baseUrl = factory.BaseUrl;
        var context = await browser.Browser.NewContextAsync(new() { BaseURL = baseUrl, Locale = "ru-RU" });
        await context.RouteAsync("**/*", async route =>
        {
            var url = route.Request.Url;
            if (url.StartsWith(baseUrl))
            {
                await route.ContinueAsync();
            }
            else if (factory.Store.GetByUrl(url) is { } content)
            {
                await route.FulfillAsync(new() { BodyBytes = content, ContentType = "image/webp" });
            }
            else
            {
                await route.AbortAsync();
            }
        });
        var page = await context.NewPageAsync();
        var testPage = new TestPage(context, page, baseUrl);
        page.PageError += (_, error) => testPage.Errors.Add(error);
        page.Console += (_, message) =>
        {
            // blocked external requests are expected
            if (message.Type == "error" && !message.Text.StartsWith("Failed to load resource"))
            {
                testPage.Errors.Add(message.Text);
            }
        };
        return testPage;
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}

/// <summary>
/// App listening on a real port, for the browser.
/// </summary>
public interface LiveAppFactory<T> where T : class
{
    string BaseUrl { get; }
    FakePictureStore Store { get; }
}

public static class LiveServer
{
    public static string Start<T>(AppFactory<T> factory) where T : class
    {
        factory.StartServer();
        return factory.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
    }
}

public class Fennica3LiveFactory : Fennica3Factory, LiveAppFactory<global::Fennica3.Fennica3>
{
    private readonly Lazy<string> _baseUrl;
    public string BaseUrl => _baseUrl.Value;

    public Fennica3LiveFactory()
    {
        UseKestrel(0);
        _baseUrl = new(() => LiveServer.Start(this));
    }
}

public class KoTiLiveFactory : KoTiFactory, LiveAppFactory<KoTi.ModelFactories.PostViewModelFactory>
{
    private readonly Lazy<string> _baseUrl;
    public string BaseUrl => _baseUrl.Value;

    public KoTiLiveFactory()
    {
        UseKestrel(0);
        _baseUrl = new(() => LiveServer.Start(this));
    }
}
