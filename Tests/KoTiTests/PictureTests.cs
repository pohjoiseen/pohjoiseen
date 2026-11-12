using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Holvi;
using Tests.Infrastructure;
using static Tests.Infrastructure.TestData;

namespace Tests.KoTiTests;

public class PictureTests(KoTiFactory factory) : IClassFixture<KoTiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private record UploadResponse(int Id, string Title, string Src, string FullscreenUrl, bool IsDuplicate,
        bool IsReplaced);

    private Task<HttpResponseMessage> PostUpload(byte[] content, string filename, string query) =>
        _client.PostAsync($"/Pictures/Upload/{Convert.ToHexStringLower(SHA1.HashData(content))}/{Uri.EscapeDataString(filename)}{query}",
            new ByteArrayContent(content), Ct);

    private async Task<UploadResponse> Upload(byte[] content, string filename, string query = "")
    {
        var response = await PostUpload(content, filename, query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<UploadResponse>(Ct))!;
    }

    private byte[] Stored(string url) => factory.Store.GetByUrl(url)!;

    [Fact]
    public async Task UploadJpeg()
    {
        var content = TestImages.JpegWithExif();
        var result = await Upload(content, "photo.jpg", "?setName=Uploads");
        Assert.False(result.IsDuplicate);

        await using var db = factory.OpenDb();
        var picture = await db.Pictures.Include(p => p.Set).SingleAsync(p => p.Id == result.Id, Ct);
        var hash = picture.Hash;
        Assert.Equal(("photo.jpg", 1200, 800, content.Length), (picture.Filename, picture.Width, picture.Height, picture.Size));
        Assert.Equal("Uploads", picture.Set!.Name);
        Assert.Equal(new DateTime(2019, 8, 1, 12, 34, 56, DateTimeKind.Local).ToUniversalTime(), picture.PhotographedAt);
        Assert.Equal(60.175, picture.Lat!.Value, 6);
        Assert.Equal(-24.95, picture.Lng!.Value, 6);
        Assert.Equal(("NIKON D7500", "18-140mm"), (picture.Camera, picture.Lens));
        Assert.False(picture.WebsiteSizesExist);

        // original as is, thumbnail and details sizes as lossy WebP
        Assert.Equal($"{Url}{hash}/photo.jpg", picture.Url);
        Assert.Equal(content, factory.Store.GetByUrl(picture.Url));
        Assert.Equal($"{Url}{hash}/photo.t.webp", picture.ThumbnailUrl);
        Assert.Equal((375, 250), TestImages.Size(Stored(picture.ThumbnailUrl)));
        Assert.Equal($"{Url}{hash}/photo.d.webp", picture.DetailsUrl);
        Assert.Equal((1000, 666), TestImages.Size(Stored(picture.DetailsUrl)));
        Assert.True(PictureImage.IsLossyWebp(Stored(picture.DetailsUrl)));
        // these keep EXIF data and color profile
        Assert.Equal("NIKON D7500", new PictureImage(Stored(picture.ThumbnailUrl)).ReadExif().Camera);
        Assert.Equal("2019:08:01 12:34:56", new PictureImage(Stored(picture.DetailsUrl)).ReadExif().DateTimeOriginal);
        Assert.True(TestImages.HasColorProfile(Stored(picture.DetailsUrl)));
        Assert.Equal("image/webp", factory.Store.Objects[$"{hash}/photo.d.webp"].ContentType);
        Assert.Equal(picture.DetailsUrl, result.Src);

        // uploading again is detected
        var again = await Upload(content, "photo.jpg");
        Assert.True(again.IsDuplicate);
        Assert.Equal(result.Id, again.Id);
        Assert.Equal(1, await db.Pictures.CountAsync(p => p.Hash == hash, Ct));

        // also under another name, or to another folder
        var renamed = await Upload(content, "copy of photo.jpg", "?setName=Elsewhere");
        Assert.True(renamed.IsDuplicate);
        Assert.Equal(result.Id, renamed.Id);
        Assert.Equal(1, await db.Pictures.CountAsync(p => p.Hash == hash, Ct));
        Assert.DoesNotContain(factory.Store.Objects.Keys, k => k.StartsWith($"{hash}/copy of photo"));

        // fullscreen view
        var fullscreen = await factory.CreateHtmxClient().GetDocumentAsync(result.FullscreenUrl);
        Assert.NotNull(fullscreen.QuerySelector($"img[src='{picture.Url}']"));
    }

    [Fact]
    public async Task UploadPng()
    {
        var result = await Upload(TestImages.Png(600, 600), "arms.png");
        await using var db = factory.OpenDb();
        var picture = await db.Pictures.SingleAsync(p => p.Id == result.Id, Ct);
        Assert.Null(picture.SetId);
        Assert.EndsWith("/arms.t.webp", picture.ThumbnailUrl);
        Assert.Equal((250, 250), TestImages.Size(Stored(picture.ThumbnailUrl)));
        Assert.False(PictureImage.IsLossyWebp(Stored(picture.ThumbnailUrl)));
        // not wide enough for details size, original is used
        Assert.Equal(picture.Url, picture.DetailsUrl);
    }

    [Fact]
    public async Task UploadNewVersion()
    {
        int setId;
        await using (var db = factory.OpenDb())
        {
            var set = new Holvi.Models.PictureSet { Name = "Versions" };
            db.PictureSets.Add(set);
            await db.SaveChangesAsync(Ct);
            setId = set.Id;
        }
        var query = $"?setId={setId}";
        var original = await Upload(TestImages.JpegWithExif(1300, 800), "photo.jpg", query);
        string oldHash;
        await using (var db = factory.OpenDb())
        {
            var picture = await db.Pictures.SingleAsync(p => p.Id == original.Id, Ct);
            picture.Title = "Kept title";
            picture.WebsiteSizesExist = true;
            picture.Website1xUrl = $"{Url}{picture.Hash}/photo.1x.webp";
            factory.Store.Put($"{picture.Hash}/photo.1x.webp", [1]);
            await db.SaveChangesAsync(Ct);
            oldHash = picture.Hash;
        }

        // same name in another folder, or exactly the same content: no conflict
        Assert.False((await Upload(TestImages.Jpeg(900, 600), "photo.jpg")).IsReplaced);
        Assert.True((await Upload(TestImages.JpegWithExif(1300, 800), "photo.jpg", query)).IsDuplicate);

        // different content with the same name in the same folder: conflict unless told what to do
        var newVersion = TestImages.Jpeg(1600, 1000);
        var conflict = await PostUpload(newVersion, "photo.jpg", query);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);

        // replace: same id, new files, title and EXIF data the new one lacks kept
        var replaced = await Upload(newVersion, "photo.jpg", query + "&onConflict=replace");
        Assert.True(replaced.IsReplaced);
        Assert.Equal(original.Id, replaced.Id);
        await using (var db = factory.OpenDb())
        {
            var picture = await db.Pictures.SingleAsync(p => p.Id == original.Id, Ct);
            Assert.Equal(Convert.ToHexStringLower(SHA1.HashData(newVersion)), picture.Hash);
            Assert.Equal(("photo.jpg", 1600, 1000, "Kept title"), (picture.Filename, picture.Width, picture.Height, picture.Title));
            Assert.Equal(newVersion, factory.Store.GetByUrl(picture.Url));
            Assert.Equal($"{Url}{picture.Hash}/photo.d.webp", picture.DetailsUrl);
            Assert.Equal(new DateTime(2019, 8, 1, 12, 34, 56, DateTimeKind.Local).ToUniversalTime(), picture.PhotographedAt);
            Assert.Equal(60.175, picture.Lat!.Value, 6);
            Assert.Equal("NIKON D7500", picture.Camera);
            Assert.False(picture.WebsiteSizesExist);
            Assert.Null(picture.Website1xUrl);
            Assert.Equal(setId, picture.SetId);
            // old files are deleted only after next publish (see PublishTests)
            Assert.Contains($"{oldHash}/photo.d.webp", factory.Store.Objects.Keys);
            Assert.Contains(oldHash, await db.PendingPictureDeletions.Select(d => d.Hash).ToListAsync(Ct));
            Assert.Equal(1, await db.Pictures.CountAsync(p => p.SetId == setId, Ct));
        }

        // rename: new pictures as photo-1.jpg, photo-2.jpg
        var renamed1 = await Upload(TestImages.Jpeg(1500, 1000), "photo.jpg", query + "&onConflict=rename");
        var renamed2 = await Upload(TestImages.Jpeg(1400, 1000), "photo.jpg", query + "&onConflict=rename");
        Assert.False(renamed1.IsReplaced);
        await using (var db = factory.OpenDb())
        {
            Assert.Equal(["photo.jpg", "photo-1.jpg", "photo-2.jpg"], await db.Pictures.Where(p => p.SetId == setId)
                .OrderBy(p => p.Id).Select(p => p.Filename).ToListAsync(Ct));
            var picture = await db.Pictures.SingleAsync(p => p.Id == renamed2.Id, Ct);
            Assert.Equal($"{Url}{picture.Hash}/photo-2.jpg", picture.Url);
        }

        // the option has no effect when there is no conflict
        var png = await Upload(TestImages.Png(300, 300), "photo.png", query + "&onConflict=replace");
        Assert.False(png.IsReplaced);
        Assert.NotEqual(original.Id, png.Id);
    }

    [Fact]
    public async Task WebSizes()
    {
        var response = await _client.PostAsync($"/api/Pictures/{NoWebSizesId}/WebSizes", null, Ct);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var db = factory.OpenDb();
        var picture = await db.Pictures.SingleAsync(p => p.Id == NoWebSizesId, Ct);
        Assert.True(picture.WebsiteSizesExist);
        Assert.Equal($"{Url}hash{NoWebSizesId}/big.1x.webp", picture.Website1xUrl);
        Assert.Equal($"{Url}hash{NoWebSizesId}/big.2x.webp", picture.Website2xUrl);
        Assert.Equal((1015, 677), TestImages.Size(Stored(picture.Website1xUrl!)));
        // (2400 * 1354 / 1600 = 2031, but scale is a double and gets truncated)
        Assert.Equal((2030, 1354), TestImages.Size(Stored(picture.Website2xUrl!)));
        Assert.True(PictureImage.IsLossyWebp(Stored(picture.Website1xUrl!)));
        // the original has EXIF data and a color profile, these keep only the latter
        Assert.Equal("NIKON D7500", new PictureImage(Stored(picture.Url)).ReadExif().Camera);
        Assert.Equal(new PictureExif(), new PictureImage(Stored(picture.Website1xUrl!)).ReadExif());
        Assert.Equal(new PictureExif(), new PictureImage(Stored(picture.Website2xUrl!)).ReadExif());
        Assert.True(TestImages.HasColorProfile(Stored(picture.Website1xUrl!)));
    }

    [Fact]
    public async Task UploadWithOrientationAndWithoutGps()
    {
        // EXIF orientation is applied: dimensions are as displayed, resized versions are turned upright
        // (the original is left as is, browsers turn it themselves)
        var result = await Upload(TestImages.JpegWithExif(1200, 800, orientation: 6, zeroGps: true), "rotated.jpg");
        await using var db = factory.OpenDb();
        var picture = await db.Pictures.SingleAsync(p => p.Id == result.Id, Ct);
        Assert.Equal((800, 1200), (picture.Width, picture.Height));
        Assert.Equal((166, 250), TestImages.Size(Stored(picture.ThumbnailUrl)));
        Assert.Equal(1, TestImages.Orientation(Stored(picture.ThumbnailUrl)));
        // not wide enough for details size anymore
        Assert.Equal(picture.Url, picture.DetailsUrl);

        // website sizes too
        var response = await _client.PostAsync($"/api/Pictures/{picture.Id}/WebSizes", null, Ct);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await db.Entry(picture).ReloadAsync(Ct);
        Assert.Equal((677, 1015), TestImages.Size(Stored(picture.Website1xUrl!)));
        Assert.Equal(1, TestImages.Orientation(Stored(picture.Website1xUrl!)));
        Assert.Null(picture.Website2xUrl);

        // a picture uploaded earlier, dimensions in database are without orientation: these get corrected
        var content = TestImages.JpegWithExif(1500, 1000, orientation: 8);
        var old = new Holvi.Models.Picture
        {
            Filename = "old.jpg", Hash = "oldrotated", Url = $"{Url}oldrotated/old.jpg", Width = 1500, Height = 1000,
            ThumbnailUrl = "", DetailsUrl = "", Size = content.Length, UploadedAt = DateTime.UtcNow
        };
        factory.Store.Put("oldrotated/old.jpg", content);
        db.Pictures.Add(old);
        await db.SaveChangesAsync(Ct);
        response = await _client.PostAsync($"/api/Pictures/{old.Id}/WebSizes", null, Ct);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await db.Entry(old).ReloadAsync(Ct);
        Assert.Equal((1000, 1500), (old.Width, old.Height));
        Assert.Equal((677, 1015), TestImages.Size(Stored(old.Website1xUrl!)));
        // 0/0 coordinates are no coordinates
        Assert.Equal((null, null), (picture.Lat, picture.Lng));
        Assert.Equal("NIKON D7500", picture.Camera);
    }

    [Fact]
    public async Task UploadWebp()
    {
        var lossy = await Upload(TestImages.Webp(1300, 700, lossless: false), "lossy.webp");
        var lossless = await Upload(TestImages.Webp(1200, 700, lossless: true), "lossless.webp");
        await using var db = factory.OpenDb();
        var picture = await db.Pictures.SingleAsync(p => p.Id == lossy.Id, Ct);
        Assert.Equal((1300, 700), (picture.Width, picture.Height));
        Assert.EndsWith("/lossy.t.webp", picture.ThumbnailUrl);
        Assert.True(PictureImage.IsLossyWebp(Stored(picture.ThumbnailUrl)));
        picture = await db.Pictures.SingleAsync(p => p.Id == lossless.Id, Ct);
        Assert.EndsWith("/lossless.d.webp", picture.DetailsUrl);
        Assert.False(PictureImage.IsLossyWebp(Stored(picture.DetailsUrl)));
        Assert.Equal((1000, 583), TestImages.Size(Stored(picture.DetailsUrl)));
    }

    [Theory]
    [InlineData("/Pictures/All")]
    [InlineData("/Pictures/Folders")]
    [InlineData("/Pictures/Upload")]
    [InlineData("/Pictures/Upload/0")]
    public async Task Pages(string url)
    {
        await _client.GetDocumentAsync(url);
    }
}
