using System.Text.Json;
using Holvi.Models;
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
    public async Task LJCrosspost()
    {
        // expected byte counts, from the same endpoint the dialog uses
        var client = factory.CreateClient();
        async Task<int> ByteCount(string query) =>
            System.Text.Encoding.UTF8.GetByteCount(await client.GetStringAsync($"/Posts/{TurkuId}/ru/LJ/Html?{query}", Ct));
        static string Bytes(int count) => count.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("en-US")) + " bytes";

        await using var t = await OpenAsync($"/Posts/{TurkuId}/ru/");
        var page = t.Page;

        await page.Locator("label[for=topleveltab-Preview]").ClickAsync();
        await page.Locator("#lj-crosspost-button").ClickAsync();
        var dialog = page.Locator("dialog#lj-crosspost-dialog");
        await Expect(dialog).ToBeVisibleAsync();
        var editor = dialog.Locator(".monaco-editor .view-lines");
        var bytes = dialog.Locator(".lj-crosspost-bytes");
        await Expect(editor).ToContainTextAsync("<lj-raw>");
        var defaultBytes = Bytes(await ByteCount(""));
        await Expect(bytes).ToHaveTextAsync(defaultBytes);

        // settings change regenerates HTML
        await dialog.Locator("select[name=Galleries]").SelectOptionAsync("LJGallery");
        await Expect(editor).ToContainTextAsync("<lj-gallery");
        var galleryBytes = await ByteCount("Galleries=LJGallery");
        await Expect(bytes).ToHaveTextAsync(Bytes(galleryBytes));

        // hand edits update byte count (Cyrillic is 2 bytes)
        await editor.ClickAsync();
        await page.Keyboard.PressAsync("Control+End");
        await page.Keyboard.TypeAsync("ёж");
        await Expect(bytes).ToHaveTextAsync(Bytes(galleryBytes + 4));

        // changing settings after hand edits asks first: cancel keeps edits and settings
        var removeNewlines = dialog.Locator("input[name=RemoveNewlines]");
        page.Dialog += DismissOnce;
        await removeNewlines.ClickAsync();
        await Expect(removeNewlines).Not.ToBeCheckedAsync();
        await Expect(editor).ToContainTextAsync("ёж");

        // ...accept overwrites
        page.Dialog += AcceptOnce;
        await removeNewlines.ClickAsync();
        await Expect(removeNewlines).ToBeCheckedAsync();
        await Expect(editor).Not.ToContainTextAsync("ёж");
        await Expect(bytes).ToHaveTextAsync(Bytes(await ByteCount("Galleries=LJGallery&RemoveNewlines=true")));

        // closing removes the dialog, opening again starts with defaults
        await dialog.Locator(".cancel-btn").ClickAsync();
        await Expect(page.Locator("koti-lj-crosspost")).ToHaveCountAsync(0);
        await page.Locator("#lj-crosspost-button").ClickAsync();
        await Expect(dialog.Locator("select[name=Galleries]")).ToHaveValueAsync("Individual");
        await Expect(bytes).ToHaveTextAsync(defaultBytes);

        Assert.Empty(t.Errors);
        return;

        async void DismissOnce(object? sender, IDialog d)
        {
            page.Dialog -= DismissOnce;
            await d.DismissAsync();
        }

        async void AcceptOnce(object? sender, IDialog d)
        {
            page.Dialog -= AcceptOnce;
            await d.AcceptAsync();
        }
    }

    private async Task CreateFoldersAsync()
    {
        await using var db = factory.OpenDb();
        if (await db.PictureSets.AnyAsync(ps => ps.Name == "Lapland", Ct)) return;
        db.PictureSets.AddRange(new PictureSet { Name = "Lapland" }, new PictureSet { Name = "Lapinlahti" },
            new PictureSet { Name = "Helsinki" });
        await db.SaveChangesAsync(Ct);
    }

    // Search box filters folders, and only the results below it are reloaded: the input itself stays
    // (same element, focus and everything typed while a request was running)
    private static async Task SearchFoldersAsync(IPage page, ILocator panel)
    {
        var input = panel.Locator("input.picture-set-search-input");
        var folders = panel.Locator(".list button.folder");
        await Expect(folders).ToHaveCountAsync(3);
        await input.EvaluateAsync("el => el.dataset.testMarker = 'kept'");

        await input.PressSequentiallyAsync("lap");
        await Expect(folders).ToHaveCountAsync(2);
        await Expect(folders.Nth(0)).ToContainTextAsync("Lapinlahti");
        await Expect(input).ToBeFocusedAsync();
        await page.Keyboard.TypeAsync("l");
        await Expect(folders).ToHaveCountAsync(1);
        await Expect(folders).ToContainTextAsync("Lapland");
        await Expect(input).ToHaveValueAsync("lapl");
        await Expect(input).ToBeFocusedAsync();
        await Expect(input).ToHaveAttributeAsync("data-test-marker", "kept");
        // pictures without folder are still shown
        await Expect(panel.Locator(".list koti-picture")).Not.ToHaveCountAsync(0);
    }

    [Fact]
    public async Task SearchPictureFoldersInEditor()
    {
        await CreateFoldersAsync();
        await using var t = await OpenAsync($"/Posts/{TurkuId}/ru/");
        var page = t.Page;
        await page.Locator("label[for=topleveltab-Insert]").ClickAsync();
        var panel = page.Locator(".insert-picture .picture-list");

        await SearchFoldersAsync(page, panel);

        // going into a folder and back up keeps the search (remembered per panel)
        await panel.Locator("button.folder").ClickAsync();
        await Expect(panel.Locator("h3")).ToContainTextAsync("Lapland");
        await panel.Locator("h3 button").ClickAsync();
        await Expect(panel.Locator("input.picture-set-search-input")).ToHaveValueAsync("lapl");
        await Expect(panel.Locator(".list button.folder")).ToHaveCountAsync(1);

        Assert.Empty(t.Errors);
    }

    [Fact]
    public async Task SearchPictureFoldersInBrowser()
    {
        await CreateFoldersAsync();
        await using var t = await OpenAsync("/Pictures/Folders");
        var page = t.Page;
        var panel = page.Locator("#browse-pictures");

        await SearchFoldersAsync(page, panel);
        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("setSearch=lapl"));

        // Back from a folder restores the search, also in the input
        await panel.Locator("button.folder").ClickAsync();
        await Expect(panel.Locator("h3")).ToContainTextAsync("Lapland");
        await page.GoBackAsync();
        await Expect(panel.Locator("input.picture-set-search-input")).ToHaveValueAsync("lapl");
        await Expect(panel.Locator(".list button.folder")).ToHaveCountAsync(1);

        // and so does a full page load
        await page.ReloadAsync();
        await Expect(panel.Locator("input.picture-set-search-input")).ToHaveValueAsync("lapl");
        await Expect(panel.Locator(".list button.folder")).ToHaveCountAsync(1);

        Assert.Empty(t.Errors);
    }

    [Theory]
    [InlineData("/Posts/ru/", "turku")]
    [InlineData("/Articles/ru/", "about")]
    [InlineData("/Books/ru/", "lapland")]
    public async Task SearchContent(string url, string query)
    {
        await using var t = await OpenAsync(url);
        var page = t.Page;
        var input = page.Locator("input.picture-set-search-input");
        var items = page.Locator(".list koti-content-item");
        await Expect(items).Not.ToHaveCountAsync(0);
        await input.EvaluateAsync("el => el.dataset.testMarker = 'kept'");

        await input.PressSequentiallyAsync(query);
        await Expect(items).ToHaveCountAsync(1);
        await Expect(items).ToHaveAttributeAsync("name", new System.Text.RegularExpressions.Regex(query));
        await Expect(input).ToHaveValueAsync(query);
        await Expect(input).ToBeFocusedAsync();
        await Expect(input).ToHaveAttributeAsync("data-test-marker", "kept");

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
