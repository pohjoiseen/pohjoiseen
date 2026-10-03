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

    private record UploadResponse(int Id, string Title, string Src, string FullscreenUrl, bool IsDuplicate);

    private async Task<UploadResponse> Upload(byte[] content, string filename, string query = "")
    {
        var hash = Convert.ToHexStringLower(SHA1.HashData(content));
        var response = await _client.PostAsync($"/Pictures/Upload/{hash}/{Uri.EscapeDataString(filename)}{query}",
            new ByteArrayContent(content), Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<UploadResponse>(Ct))!;
    }

    private ImageInfo Identify(string url) => Image.Identify(factory.Store.GetByUrl(url)!);

    private static byte[] JpegWithExif()
    {
        using var image = new Image<Rgb24>(1200, 800, new Rgb24(10, 100, 200));
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

        // fullscreen view
        var fullscreen = await factory.CreateHtmxClient().GetDocumentAsync(result.FullscreenUrl);
        Assert.NotNull(fullscreen.QuerySelector($"img[src='{picture.Url}']"));
    }

    [Fact]
    public async Task UploadPng()
    {
        var result = await Upload(TestImages.Png(600, 600), "coat.png");
        await using var db = factory.OpenDb();
        var picture = await db.Pictures.SingleAsync(p => p.Id == result.Id, Ct);
        Assert.Null(picture.SetId);
        Assert.EndsWith("/coat.t.webp", picture.ThumbnailUrl);
        Assert.Equal(WebpFileFormatType.Lossless, Identify(picture.ThumbnailUrl).Metadata.GetWebpMetadata().FileFormat);
        // not wide enough for details size, original is used
        Assert.Equal(picture.Url, picture.DetailsUrl);
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
