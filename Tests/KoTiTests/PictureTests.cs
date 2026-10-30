using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
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

    private ImageInfo Identify(string url) => Image.Identify(factory.Store.GetByUrl(url)!);

    private static byte[] JpegWithExif(int width = 1200, int height = 800)
    {
        using var image = new Image<Rgb24>(width, height, new Rgb24(10, 100, 200));
        var exif = new ExifProfile();
        exif.SetValue(ExifTag.DateTimeOriginal, "2019:08:01 12:34:56");
        exif.SetValue(ExifTag.GPSLatitude, [new Rational(60, 1), new Rational(10, 1), new Rational(3000, 100)]);
        exif.SetValue(ExifTag.GPSLatitudeRef, "N");
        exif.SetValue(ExifTag.GPSLongitude, [new Rational(24, 1), new Rational(57, 1), new Rational(0, 1)]);
        exif.SetValue(ExifTag.GPSLongitudeRef, "W");
        exif.SetValue(ExifTag.Model, "NIKON D7500");
        exif.SetValue(ExifTag.LensModel, "18-140mm");
        image.Metadata.ExifProfile = exif;
        using var output = new MemoryStream();
        image.Save(output, new JpegEncoder());
        return output.ToArray();
    }

    [Fact]
    public async Task UploadJpeg()
    {
        var content = JpegWithExif();
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
        Assert.Equal((375, 250), (Identify(picture.ThumbnailUrl).Width, Identify(picture.ThumbnailUrl).Height));
        Assert.Equal($"{Url}{hash}/photo.d.webp", picture.DetailsUrl);
        Assert.Equal(1000, Identify(picture.DetailsUrl).Width);
        Assert.Equal(WebpFileFormatType.Lossy, Identify(picture.DetailsUrl).Metadata.GetWebpMetadata().FileFormat);
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
        Assert.Equal(WebpFileFormatType.Lossless, Identify(picture.ThumbnailUrl).Metadata.GetWebpMetadata().FileFormat);
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
        var original = await Upload(JpegWithExif(1300, 800), "photo.jpg", query);
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
        Assert.True((await Upload(JpegWithExif(1300, 800), "photo.jpg", query)).IsDuplicate);

        // different content with the same name in the same folder: conflict unless told what to do
        var newVersion = TestImages.Jpeg(1600, 1000);
        var conflict = await PostUpload(newVersion, "photo.jpg", query);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);

        // replace: same id, new files, old files deleted, title and EXIF data the new one lacks kept
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
            Assert.DoesNotContain(factory.Store.Objects.Keys, k => k.StartsWith(oldHash + "/"));
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
        Assert.Equal((1015, 677), (Identify(picture.Website1xUrl!).Width, Identify(picture.Website1xUrl!).Height));
        // (2400 * 1354 / 1600 = 2031, but scale is a double and gets truncated)
        Assert.Equal((2030, 1354), (Identify(picture.Website2xUrl!).Width, Identify(picture.Website2xUrl!).Height));
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
