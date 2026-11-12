using Holvi;
using Holvi.Models;
using Tests.Infrastructure;

namespace Tests.UnitTests;

public class PictureTests
{
    [Theory]
    [InlineData(3000, 2000, 677, 1015, 677)]   // landscape: height fits
    [InlineData(2000, 3000, 677, 677, 1015)]   // portrait: width fits
    [InlineData(1000, 1000, 677, 677, 677)]
    [InlineData(800, 600, 677, 0, 0)]          // too small
    [InlineData(677, 677, 677, 677, 677)]      // exactly
    public void DownsizedDimensions(int width, int height, int target, int expectedWidth, int expectedHeight)
    {
        var picture = new Picture { Width = width, Height = height };
        Assert.Equal((expectedWidth, expectedHeight), picture.GetDownsizedDimensions(target));
    }

    [Theory]
    [InlineData("a/b.jpg", "image/jpeg")]
    [InlineData("a/b.JPEG", "image/jpeg")]
    [InlineData("a/b.png", "image/png")]
    [InlineData("a/b.t.webp", "image/webp")]
    public void ContentType(string name, string expected)
    {
        Assert.Equal(expected, PictureStorage.GetContentType(name));
    }

    [Fact]
    public void ImageInfo()
    {
        Assert.Equal((300, 200, true), Info(TestImages.Jpeg(300, 200)));
        Assert.Equal((300, 200, false), Info(TestImages.Png(300, 200)));
        Assert.Equal((300, 200, true), Info(TestImages.Webp(300, 200, lossless: false)));
        Assert.Equal((300, 200, false), Info(TestImages.Webp(300, 200, lossless: true)));
        Assert.ThrowsAny<Exception>(() => new PictureImage("not an image"u8.ToArray()));

        static (int, int, bool) Info(byte[] content)
        {
            var image = new PictureImage(content);
            return (image.Width, image.Height, image.IsLossy);
        }
    }

    [Fact]
    public void Exif()
    {
        Assert.Equal(new PictureExif
        {
            DateTimeOriginal = "2019:08:01 12:34:56",
            Camera = "NIKON D7500",
            Lens = "18-140mm",
            Lat = 60.175,
            Lng = -24.95
        }, new PictureImage(TestImages.JpegWithExif(60, 40)).ReadExif());
        Assert.Equal(new PictureExif { DateTimeOriginal = "2019:08:01 12:34:56", Camera = "NIKON D7500", Lens = "18-140mm" },
            new PictureImage(TestImages.JpegWithExif(60, 40, zeroGps: true)).ReadExif());
        Assert.Equal(new PictureExif(), new PictureImage(TestImages.Jpeg(60, 40)).ReadExif());
        Assert.Equal(new PictureExif(), new PictureImage(TestImages.Png(60, 40)).ReadExif());
    }

    [Theory]
    [InlineData(null, 300, 200)]
    [InlineData(1, 300, 200)]
    [InlineData(3, 300, 200)]   // upside down
    [InlineData(6, 200, 300)]   // on its side
    [InlineData(8, 200, 300)]   // on its other side
    public void Orientation(int? orientation, int expectedWidth, int expectedHeight)
    {
        var image = new PictureImage(TestImages.JpegWithExif(300, 200, orientation: (ushort?)orientation));
        Assert.Equal((expectedWidth, expectedHeight), (image.Width, image.Height));

        // resized versions are turned upright and so do not have orientation anymore; other EXIF data is still there
        var (content, _) = image.Resize(expectedWidth / 2, expectedHeight / 2, false);
        Assert.Equal((expectedWidth / 2, expectedHeight / 2), TestImages.Size(content));
        Assert.Equal(1, TestImages.Orientation(content));
        Assert.Equal("NIKON D7500", new PictureImage(content).ReadExif().Camera);
        // (the test picture itself is upright, so with orientation it comes out turned)
        Assert.Equal(orientation is null or 1, TestImages.IsUpright(content));

        (content, _) = image.Resize(expectedWidth / 2, expectedHeight / 2, true);
        Assert.Equal((expectedWidth / 2, expectedHeight / 2), TestImages.Size(content));
        Assert.Equal(1, TestImages.Orientation(content));
    }

    [Theory]
    [InlineData("18-140mm", "NIKON D7500", "18-140mm")]
    [InlineData("70-300mm", "NIKON D7500", "70-300mm")]   // was "-300mm" once: characters of camera name were trimmed
    [InlineData("iPhone 6 back camera 4.15mm f/2.2", "iPhone 6", "back camera 4.15mm f/2.2")]
    [InlineData("CAM_50mm", "cam", "50mm")]
    [InlineData("X100V", "X100V", "X100V")]
    [InlineData("50mm", null, "50mm")]
    [InlineData("50mm", " ", "50mm")]
    [InlineData(null, "NIKON D7500", null)]
    public void LensWithoutCamera(string? lens, string? camera, string? expected)
    {
        Assert.Equal(expected, PictureUpload.GetLensWithoutCamera(lens, camera));
    }

    [Theory]
    [InlineData(false, ".webp", ".webp")]
    [InlineData(true, ".jpg", ".png")]   // too large for WebP
    public void ResizeFormats(bool panorama, string expectedForLossy, string expectedForLossless)
    {
        var (width, height) = panorama ? (20000, 300) : (800, 600);
        var (toWidth, toHeight) = panorama ? (16400, 246) : (400, 300);

        var (content, extension) = new PictureImage(TestImages.JpegWithExif(width, height)).Resize(toWidth, toHeight, false);
        Assert.Equal(expectedForLossy, extension);
        Assert.Equal((toWidth, toHeight), TestImages.Size(content));
        Assert.Equal("NIKON D7500", new PictureImage(content).ReadExif().Camera);
        Assert.True(TestImages.HasColorProfile(content));
        Assert.True(new PictureImage(content).IsLossy);

        (content, extension) = new PictureImage(TestImages.JpegWithExif(width, height)).Resize(toWidth, toHeight, true);
        Assert.Equal(expectedForLossy, extension);
        Assert.Equal(new PictureExif(), new PictureImage(content).ReadExif());
        Assert.True(TestImages.HasColorProfile(content));

        (content, extension) = new PictureImage(TestImages.Png(width, height)).Resize(toWidth, toHeight, false);
        Assert.Equal(expectedForLossless, extension);
        Assert.Equal((toWidth, toHeight), TestImages.Size(content));
        Assert.False(new PictureImage(content).IsLossy);
    }
}
