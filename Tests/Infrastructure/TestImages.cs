using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Tests.Infrastructure;

public static class TestImages
{
    public static byte[] Jpeg(int width, int height) => Encode(width, height, new JpegEncoder());

    public static byte[] Png(int width, int height) => Encode(width, height, new PngEncoder());

    private static byte[] Encode(int width, int height, SixLabors.ImageSharp.Formats.IImageEncoder encoder)
    {
        // a gradient rather than a flat color, so that the encoders have something to do
        using var image = new Image<Rgba32>(width, height);
        image.ProcessPixelRows(rows =>
        {
            for (var y = 0; y < rows.Height; y++)
            {
                var row = rows.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    row[x] = new Rgba32((byte)(x * 255 / width), (byte)(y * 255 / height), 128);
                }
            }
        });
        using var output = new MemoryStream();
        image.Save(output, encoder);
        return output.ToArray();
    }
}
