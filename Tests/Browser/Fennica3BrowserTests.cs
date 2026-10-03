using Microsoft.Playwright;
using Tests.Infrastructure;
using static Microsoft.Playwright.Assertions;
using static Tests.Infrastructure.TestData;

namespace Tests.Browser;

/// <summary>
/// Fennica3 client-side code (maps, galleries) in a real browser.
/// </summary>
public class Fennica3BrowserTests(BrowserFixture browser, Fennica3LiveFactory factory) : IClassFixture<Fennica3LiveFactory>
{
    [Fact]
    public async Task MapWithPopups()
    {
        await using var t = await TestPage.CreateAsync(browser, factory);
        await t.Page.GotoAsync("/ru/");

        // first map is shown and initialized
        var map = t.Page.Locator(".leaflet-container[data-map=index]");
        await Expect(map).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("leaflet-touch|leaflet-grab"));
        await Expect(t.Page.Locator(".leaflet-container[data-map=osm]")).ToBeHiddenAsync();

        // Turku is visible on all zoom levels; click loads details from post JSON into popup
        var marker = map.Locator(".leaflet-marker-icon").First;
        await marker.ClickAsync();
        var popup = map.Locator(".leaflet-popup");
        await Expect(popup).ToContainTextAsync("Турку");
        await Expect(popup).ToContainTextAsync("Бывшая столица.");
        await Expect(popup).Not.ToContainTextAsync("Error");

        // switch to the other map
        await map.Locator("button[data-map=osm]").ClickAsync();
        await Expect(t.Page.Locator(".leaflet-container[data-map=osm]")).ToBeVisibleAsync();
        await Expect(map).ToBeHiddenAsync();

        Assert.Empty(t.Errors);
    }

    [Fact]
    public async Task Gallery()
    {
        await using var t = await TestPage.CreateAsync(browser, factory);
        await t.Page.GotoAsync(TurkuPath);

        var gallery = t.Page.Locator(".glider-contain");
        await Expect(gallery.Locator(".glider-dot")).ToHaveCountAsync(3);
        await Expect(gallery.Locator(".glider-slide.active")).ToContainTextAsync("Собор");

        // (arrow icons come from Font Awesome, which is blocked in tests, so the buttons have no size to click)
        await gallery.Locator(".glider-next").DispatchEventAsync("click");
        await Expect(gallery.Locator(".glider-slide.active")).ToContainTextAsync("Замок");
        await Expect(gallery.Locator(".glider-dot.active")).ToHaveAttributeAsync("data-index", "1");

        // pictures are loaded (from fake storage)
        await Expect(gallery.Locator(".glider-slide.active img")).ToHaveJSPropertyAsync("naturalWidth", 30);

        Assert.Empty(t.Errors);
    }

    [Fact]
    public async Task KeyboardNavigation()
    {
        await using var t = await TestPage.CreateAsync(browser, factory);
        await t.Page.GotoAsync(HelsinkiPath);
        await t.Page.Keyboard.PressAsync("Control+ArrowRight");
        await Expect(t.Page).ToHaveURLAsync(factory.BaseUrl + TurkuPath);
        await t.Page.WaitForLoadStateAsync();
        await t.Page.Keyboard.PressAsync("Control+ArrowLeft");
        await Expect(t.Page).ToHaveURLAsync(factory.BaseUrl + HelsinkiPath);
        Assert.Empty(t.Errors);
    }
}
