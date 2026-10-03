using Holvi;
using Holvi.Models;

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
}
