using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Teos;

/// <summary>
/// Classic Markdown with YAML front matter content loader.
/// This is just some glue code for Markdig and YamlDotNet libraries.
/// TODO: this does make some hardcoded assumptions, e. g. UnderscoredNamingConvention; allow to configure?
/// </summary>
public class MarkdownLoader : IContentLoader
{
    private IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private MarkdownPipeline _markdownPipeline = new MarkdownPipelineBuilder()
        .UseYamlFrontMatter()
        .Build();

    public async Task<Content> LoadContent(string filename, Stream stream, Type type)
    {
        Content content;

        // parse Markdown
        var reader = new StreamReader(stream);
        var mdDocument = Markdown.Parse(await reader.ReadToEndAsync(), _markdownPipeline);

        // retrieve and deserialize YAML front matter
        var block = mdDocument.FirstOrDefault(b => b is YamlFrontMatterBlock);
        if (block != null)
        {
            LeafBlock leafBlock = (LeafBlock) block;
            string yaml = leafBlock.Lines.ToString();
            content = (Content) _deserializer.Deserialize(yaml, type)!;
            mdDocument.Remove(block);
        }
        else
        {
            // front matter is optional in principle, just create a new empty instance of content if there's none
            content = (Content) Activator.CreateInstance(type)!;
        }

        // the rest is markdown, which we convert to HTML
        content.HTML = mdDocument.ToHtml();
        content.Name = filename;

        return content;
    }
}