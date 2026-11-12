using System.Buffers.Binary;
using System.Text;
using NetVips;

namespace Tests.Infrastructure;

public static class TestImages
{
    public static byte[] Jpeg(int width, int height) => Gradient(width, height, 3).JpegsaveBuffer();

    public static byte[] Png(int width, int height) => Gradient(width, height, 4).PngsaveBuffer();

    public static byte[] Webp(int width, int height, bool lossless) =>
        Gradient(width, height, 3).WebpsaveBuffer(lossless: lossless);

    /// <summary>
    /// JPEG with an embedded sRGB profile and EXIF data: taken 2019-08-01 12:34:56 at 60°10'30"N 24°57'W
    /// with NIKON D7500 and 18-140mm lens.
    /// </summary>
    /// <param name="orientation">EXIF orientation, if any</param>
    /// <param name="zeroGps">Coordinates are present but as 0/0, like some cameras do without GPS fix</param>
    public static byte[] JpegWithExif(int width = 1200, int height = 800, ushort? orientation = null,
        bool zeroGps = false)
    {
        var jpeg = Gradient(width, height, 3).JpegsaveBuffer(profile: "srgb");
        var exif = Exif(orientation, zeroGps);
        // APP1 segment right after SOI
        var segment = new byte[4 + 6 + exif.Length];
        segment[0] = 0xFF;
        segment[1] = 0xE1;
        BinaryPrimitives.WriteUInt16BigEndian(segment.AsSpan(2), (ushort)(2 + 6 + exif.Length));
        "Exif\0\0"u8.CopyTo(segment.AsSpan(4));
        exif.CopyTo(segment.AsSpan(10));
        return [.. jpeg[..2], .. segment, .. jpeg[2..]];
    }

    public static (int Width, int Height) Size(byte[] content)
    {
        using var image = Image.NewFromBuffer(content);
        return (image.Width, image.Height);
    }

    /// <summary>EXIF orientation, 1 (upright) if there is none</summary>
    public static int Orientation(byte[] content)
    {
        using var image = Image.NewFromBuffer(content);
        return image.Contains("orientation") ? (int)image.Get("orientation") : 1;
    }

    /// <summary>
    /// Whether the picture is an upright one of ours: red grows from left to right, green from top to bottom
    /// </summary>
    public static bool IsUpright(byte[] content)
    {
        using var image = Image.NewFromBuffer(content);
        var topLeft = image.Getpoint(2, 2);
        var topRight = image.Getpoint(image.Width - 3, 2);
        var bottomLeft = image.Getpoint(2, image.Height - 3);
        return topRight[0] - topLeft[0] > 100 && bottomLeft[1] - topLeft[1] > 100 &&
               Math.Abs(topRight[1] - topLeft[1]) < 50 && Math.Abs(bottomLeft[0] - topLeft[0]) < 50;
    }

    public static bool HasColorProfile(byte[] content)
    {
        using var image = Image.NewFromBuffer(content);
        return image.Contains("icc-profile-data");
    }

    private static Image Gradient(int width, int height, int bands)
    {
        // a gradient rather than a flat color, so that the encoders have something to do
        var pixels = new byte[width * height * bands];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var i = (y * width + x) * bands;
                pixels[i] = (byte)(x * 255L / width);
                pixels[i + 1] = (byte)(y * 255L / height);
                pixels[i + 2] = 128;
                if (bands == 4)
                {
                    pixels[i + 3] = 255;
                }
            }
        }
        return Image.NewFromMemory(pixels, width, height, bands, Enums.BandFormat.Uchar)
            .Copy(interpretation: Enums.Interpretation.Srgb);
    }

    // EXIF is written by hand, so that reading it is tested against the format and not against the same library

    private record struct Entry(ushort Tag, ushort Type, uint Count, byte[] Data);

    private static Entry Ascii(ushort tag, string value)
    {
        var data = Encoding.ASCII.GetBytes(value + "\0");
        return new Entry(tag, 2, (uint)data.Length, data);
    }

    private static Entry Short(ushort tag, ushort value)
    {
        var data = new byte[4];
        BinaryPrimitives.WriteUInt16LittleEndian(data, value);
        return new Entry(tag, 3, 1, data);
    }

    private static Entry Long(ushort tag, uint value)
    {
        var data = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(data, value);
        return new Entry(tag, 4, 1, data);
    }

    private static Entry Rationals(ushort tag, params (uint Numerator, uint Denominator)[] values)
    {
        var data = new byte[values.Length * 8];
        for (var i = 0; i < values.Length; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(i * 8), values[i].Numerator);
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(i * 8 + 4), values[i].Denominator);
        }
        return new Entry(tag, 5, (uint)values.Length, data);
    }

    private static int IfdSize(IEnumerable<Entry> entries) =>
        2 + 4 + entries.Sum(e => 12 + (e.Data.Length > 4 ? e.Data.Length + e.Data.Length % 2 : 0));

    private static byte[] Exif(ushort? orientation, bool zeroGps)
    {
        Entry[] exifIfd = [Ascii(0x9003, "2019:08:01 12:34:56"), Ascii(0xA434, "18-140mm")];
        Entry[] gpsIfd = zeroGps
            ?
            [
                Ascii(1, "N"), Rationals(2, (0, 0), (0, 0), (0, 0)),
                Ascii(3, "E"), Rationals(4, (0, 0), (0, 0), (0, 0))
            ]
            :
            [
                Ascii(1, "N"), Rationals(2, (60, 1), (10, 1), (3000, 100)),
                Ascii(3, "W"), Rationals(4, (24, 1), (57, 1), (0, 1))
            ];

        // tags within an IFD must be in ascending order; pointers to other IFDs are filled in once sizes are known
        List<Entry> Ifd0(uint exifOffset, uint gpsOffset)
        {
            List<Entry> entries = [Ascii(0x0110, "NIKON D7500")];
            if (orientation is not null)
            {
                entries.Add(Short(0x0112, orientation.Value));
            }
            entries.Add(Long(0x8769, exifOffset));
            entries.Add(Long(0x8825, gpsOffset));
            return entries;
        }

        var exifOffset = 8 + IfdSize(Ifd0(0, 0));
        var gpsOffset = exifOffset + IfdSize(exifIfd);
        var output = new MemoryStream();
        var writer = new BinaryWriter(output);
        writer.Write("II*\0"u8);
        writer.Write(8u);
        foreach (var ifd in new IEnumerable<Entry>[] { Ifd0((uint)exifOffset, (uint)gpsOffset), exifIfd, gpsIfd })
        {
            var entries = ifd.ToList();
            var dataOffset = (int)output.Position + 2 + entries.Count * 12 + 4;
            var data = new MemoryStream();
            writer.Write((ushort)entries.Count);
            foreach (var entry in entries)
            {
                writer.Write(entry.Tag);
                writer.Write(entry.Type);
                writer.Write(entry.Count);
                if (entry.Data.Length > 4)
                {
                    writer.Write((uint)(dataOffset + data.Position));
                    data.Write(entry.Data);
                    if (entry.Data.Length % 2 == 1)
                    {
                        data.WriteByte(0);
                    }
                }
                else
                {
                    writer.Write(entry.Data);
                    writer.Write(new byte[4 - entry.Data.Length]);
                }
            }
            writer.Write(0u);
            writer.Write(data.ToArray());
        }
        return output.ToArray();
    }
}
