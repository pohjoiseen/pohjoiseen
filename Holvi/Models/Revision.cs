using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Text;

namespace Holvi.Models;

/// <summary>
/// An earlier text (ContentMD) of a post, article or book, recorded by KoTi when the edit page is saved.
/// Not every save makes a new revision, see RevisionRecorder in KoTi.  Text is stored compressed.
/// </summary>
public class Revision
{
    public int Id { get; set; }

    /// <summary>
    /// Same as in <see cref="Suggestion"/>: not a foreign key, revisions are deleted with the target in KoTi.
    /// </summary>
    [StringLength(16)]
    public string TargetType { get; set; } = "";

    public int TargetId { get; set; }

    /// <summary>
    /// Version of the post/article/book which had this text (the last one, if several saves went into this revision).
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// ContentMD, UTF-8, gzip.
    /// </summary>
    public byte[] ContentGz { get; set; } = [];

    /// <summary>
    /// Length of the text in characters.
    /// </summary>
    public int ContentLength { get; set; }

    /// <summary>
    /// When this revision was started, saves during the following hour go into it.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the text of this revision was saved (last).
    /// </summary>
    public DateTime SavedAt { get; set; }

    /// <summary>
    /// Never overwritten by later saves or removed by thinning: text which was published,
    /// or which something else refers to.
    /// </summary>
    public bool Keep { get; set; }

    public string GetContent()
    {
        using var input = new GZipStream(new MemoryStream(ContentGz), CompressionMode.Decompress);
        using var reader = new StreamReader(input, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public void SetContent(string content)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
        {
            gzip.Write(Encoding.UTF8.GetBytes(content));
        }
        ContentGz = output.ToArray();
        ContentLength = content.Length;
    }
}
