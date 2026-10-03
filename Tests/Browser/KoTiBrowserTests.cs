using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Tests.Infrastructure;
using static Microsoft.Playwright.Assertions;
using static Tests.Infrastructure.TestData;

namespace Tests.Browser;

/// <summary>
/// KoTi UI in a real browser.  Requires KoTi frontend to be built (cd KoTi/Frontend && bun run build).
/// </summary>
public class KoTiBrowserTests(BrowserFixture browser, KoTiLiveFactory factory) : IClassFixture<KoTiLiveFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<TestPage> OpenAsync(string url)
    {
        Assert.True(File.Exists(Path.Combine(factory.ContentRoot, "wwwroot/frontend/koti.js")),
            "KoTi frontend not built, run: cd KoTi/Frontend && bun run build");
        var t = await TestPage.CreateAsync(browser, factory);
        await t.Page.GotoAsync(url);
        return t;
    }

    [Fact]
    public async Task EditAndSavePost()
    {
        static string Json(object? o) => JsonSerializer.Serialize(o);
        string geoBefore, coatsOfArmsBefore;
        await using (var db = factory.OpenDb())
        {
            var post = await db.Posts.SingleAsync(p => p.Id == HelsinkiId, Ct);
            // (no icon is saved as default icon, same thing for maps)
            (geoBefore, coatsOfArmsBefore) = (Json(post.Geo!.Select(g => g with { Icon = g.Icon ?? "star" })), Json(post.CoatsOfArms));
        }

        await using var t = await OpenAsync($"/Posts/{HelsinkiId}/ru/");
        var page = t.Page;

        // Monaco editor with content
        var editor = page.Locator(".monaco-editor .view-lines");
        await Expect(editor).ToContainTextAsync("История города");

        // change title in properties form and append text in editor
        await page.Locator("#content-form input#title").FillAsync("Хельсинки (изменено)");
        await editor.ClickAsync();
        await page.Keyboard.PressAsync("Control+End");
        await page.Keyboard.TypeAsync("\nДобавлено в браузере.");

        // save, title in header gets updated
        var response = await page.RunAndWaitForResponseAsync(() => page.Locator("#save-button").ClickAsync(),
            r => r.Request.Method == "PUT");
        Assert.Equal(200, response.Status);
        await Expect(page.Locator("#header-title")).ToContainTextAsync("Хельсинки (изменено)");

        await using (var db = factory.OpenDb())
        {
            var post = await db.Posts.SingleAsync(p => p.Id == HelsinkiId, Ct);
            Assert.Equal("Хельсинки (изменено)", post.Title);
            Assert.EndsWith("<!--/aside-->\nДобавлено в браузере.", post.ContentMD.Replace("\r\n", "\n"));
            // everything else is kept as is, incl. what is edited by web components
            Assert.Equal("Столица **Финляндии** и её крупнейший город.", post.Description);
            Assert.Equal(LandscapeId, post.TitlePictureId);
            Assert.Equal(coatsOfArmsBefore, Json(post.CoatsOfArms));
            Assert.Equal(geoBefore, Json(post.Geo));
        }

        Assert.Empty(t.Errors);
    }

    [Fact]
    public async Task CreatePost()
    {
        await using var t = await OpenAsync("/Posts/ru/");
        var page = t.Page;

        await page.Locator("#create-button").ClickAsync();
        var dialog = page.Locator("dialog#create-post-dialog");
        await Expect(dialog).ToBeVisibleAsync();
        await dialog.Locator("#create-post-name").FillAsync("from-browser");
        await dialog.Locator("#create-post-title").FillAsync("Из браузера");
        await dialog.Locator("#create-post-submit-button").ClickAsync();

        // redirected to editor
        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Posts/[0-9]+/ru/$"));
        await Expect(page.Locator("#content-form input#title")).ToHaveValueAsync("Из браузера");

        Assert.Empty(t.Errors);
    }

    [Fact]
    public async Task UploadPictures()
    {
        await using var t = await OpenAsync("/Pictures/Upload/0");
        var page = t.Page;

        await page.Locator("input.upload-hidden-button").SetInputFilesAsync(
        [
            new FilePayload { Name = "one.jpg", MimeType = "image/jpeg", Buffer = TestImages.Jpeg(1600, 1000) },
            new FilePayload { Name = "two.png", MimeType = "image/png", Buffer = TestImages.Png(400, 300) },
            // ignored
            new FilePayload { Name = "three.txt", MimeType = "text/plain", Buffer = "not a picture"u8.ToArray() },
        ]);

        // both uploaded, one by one
        var pictures = page.Locator("koti-picture");
        await Expect(pictures).ToHaveCountAsync(2);
        await Expect(pictures.Nth(0)).ToHaveAttributeAsync("picture-id", new System.Text.RegularExpressions.Regex("[0-9]+"));
        await Expect(pictures.Nth(1)).ToHaveAttributeAsync("picture-id", new System.Text.RegularExpressions.Regex("[0-9]+"));
        await Expect(pictures.Nth(0)).ToHaveAttributeAsync("state", "");

        await using var db = factory.OpenDb();
        var uploaded = await db.Pictures.Where(p => p.Filename == "one.jpg" || p.Filename == "two.png")
            .OrderBy(p => p.Filename).ToListAsync(Ct);
        Assert.Equal(["one.jpg", "two.png"], uploaded.Select(p => p.Filename));
        Assert.Equal((1600, 1000), (uploaded[0].Width, uploaded[0].Height));
        Assert.All(uploaded, p => Assert.Null(p.SetId));

        Assert.Empty(t.Errors);
    }
}
