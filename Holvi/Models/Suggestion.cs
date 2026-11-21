using System.ComponentModel.DataAnnotations;

namespace Holvi.Models;

public enum SuggestionStatus
{
    Pending,
    Accepted,
    Declined
}

/// <summary>
/// A suggested edit to the text (ContentMD) of a post, article or book: replace OldText with NewText.
/// Created from outside through the API (AI-assisted reviewing), then accepted or declined by hand
/// on the edit page.  Anchored by exact text and not by position: applies only while OldText occurs
/// in the content exactly once.
/// </summary>
public class Suggestion
{
    public const string Post = "post";
    public const string Article = "article";
    public const string Book = "book";
    public static readonly string[] TargetTypes = [Post, Article, Book];

    /// <summary>
    /// correction: fix in place, not marked in the text; update: new development, text of the suggestion
    /// includes a visible dated note; link: only adds a link.
    /// </summary>
    public static readonly string[] Kinds = ["correction", "update", "link", "other"];

    public int Id { get; set; }

    /// <summary>
    /// Target is not a foreign key, as it is one of three tables; suggestions are deleted with the target in KoTi.
    /// </summary>
    [StringLength(16)]
    public string TargetType { get; set; } = "";

    public int TargetId { get; set; }

    [StringLength(16)]
    public string Kind { get; set; } = "";

    public string OldText { get; set; } = "";

    public string NewText { get; set; } = "";

    /// <summary>
    /// Why, for the reviewer.
    /// </summary>
    public string Comment { get; set; } = "";

    /// <summary>
    /// URLs, one per line.
    /// </summary>
    public string Sources { get; set; } = "";

    public SuggestionStatus Status { get; set; }

    /// <summary>
    /// Name of the run which created the suggestion, to find or withdraw them together.
    /// </summary>
    [StringLength(100)]
    public string Batch { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Line endings in content may be either, and differ between the database and the editor.
    /// </summary>
    public static string NormalizeText(string? text) => (text ?? "").Replace("\r\n", "\n");

    public static int CountOccurrences(string content, string text)
    {
        if (text.Length == 0)
        {
            return 0;
        }

        int count = 0, index = 0;
        while ((index = content.IndexOf(text, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += 1;
        }
        return count;
    }
}
