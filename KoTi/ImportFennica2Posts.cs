using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using KoTi.Models;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace KoTi;

// One-time use: import Fennica2 posts and convert links and images
// only posts (articles can just do manually), only Russian
// Not well-polished; slowish, post link resolving doesn't handle links with hashes
// and last post for some reason.  This was fixed manually instead of here.  Remains now for reference
public static class ImportFennica2Posts
{
    public static async Task Do(string directory, ILogger logger, KoTiDbContext dbContext, PictureUpload pictureUpload)
    {
        logger.LogInformation("Reading in post markdown files, parsing, processing images");
        
        IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        MarkdownPipeline markdownPipeline = new MarkdownPipelineBuilder()
            .UseYamlFrontMatter()
            .Build();

        var imgMap = new Dictionary<string, string>();

        var pictureSetBlogPictures = await dbContext.PictureSets.FirstOrDefaultAsync(ps => ps.Name == "Blog Pictures");
        if (pictureSetBlogPictures == null)
        {
            pictureSetBlogPictures = new PictureSet { Name = "Blog Pictures" };
            dbContext.PictureSets.Add(pictureSetBlogPictures);
        }
        
        var pictureSetCoatsOfArms = await dbContext.PictureSets.FirstOrDefaultAsync(ps => ps.Name == "Coats of Arms");
        if (pictureSetCoatsOfArms == null)
        {
            pictureSetCoatsOfArms = new PictureSet { Name = "Coats of Arms" };
            dbContext.PictureSets.Add(pictureSetCoatsOfArms);
        }


        var files = Directory.EnumerateFiles(directory, "*.ru.post.md", SearchOption.AllDirectories);
        foreach (var filename in files.OrderBy(Path.GetFileName))
        {
            // read and parse markdown
            logger.LogInformation("Loading: {File}", filename);
            var content = await File.ReadAllTextAsync(filename);
            var mdDocument = Markdown.Parse(content, markdownPipeline);
            var block = mdDocument.FirstOrDefault(b => b is YamlFrontMatterBlock);
            if (block == null)
            {
                throw new Exception("Missing front matter!");
            }

            // parse and remove YAML front matter
            LeafBlock leafBlock = (LeafBlock)block;
            string yaml = leafBlock.Lines.ToString();
            var post = (Post?)deserializer.Deserialize(yaml, typeof(Post));
            if (post == null)
            {
                throw new Exception("Deserialization failed!");
            }

            mdDocument.Remove(block);
            content = content.Substring(block.Span.End + 1);
            
            // store post content
            post.ContentMD = content.Trim();
            
            // determine date/name/language from filename
            var match = Regex.Match(filename, "([0-9]{4})-([0-9]{2})-([0-9]{2})-([^.]+)\\.([^.]+)\\.post\\.md$");
            if (match.Success)
            {
                post.Date = new DateOnly(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value));
                post.Name = match.Groups[4].Value;
                post.Language = match.Groups[5].Value;
            }
            else
            {
                throw new Exception("Failed to parse post name!");
            }

            // prefer nulls to zero-length lists
            if (post.CoatsOfArms?.Count == 0)
            {
                post.CoatsOfArms = null;
            }
            if (post.Geo?.Count == 0)
            {
                post.Geo = null;
            }

            // process all images linked to post
            var postDir = Path.GetDirectoryName(filename)!;
            post.ContentMD = new Regex("\\((\\.[^)\\s]+?\\.(jpg|png|jpeg))\\)").Replace(post.ContentMD,
                matchImage =>
                {
                    var imagePath = Path.Join(postDir, matchImage.Groups[1].Value);
                    var picture = ProcessImage(logger, dbContext, pictureUpload, pictureSetBlogPictures, imgMap,
                        imagePath, matchImage.Value);
                    return $"(picture:{picture.Id})";
                });

            // process title image, if any
            if (!string.IsNullOrEmpty(post.TitleImage))
            {
                var imagePath =  Path.Join(directory, post.TitleImage);
                var picture = ProcessImage(logger, dbContext, pictureUpload, pictureSetBlogPictures, imgMap,
                    imagePath, post.TitleImage);
                post.TitlePictureId = picture.Id;
                post.TitlePicture = picture;
                post.TitleImage = null;
            }
            
            // process coats of arms
            if (post.CoatsOfArms != null)
            {
                foreach (var coa in post.CoatsOfArms)
                {
                    var imagePath =  Path.Join(directory, coa.Url);
                    var picture = ProcessImage(logger, dbContext, pictureUpload, pictureSetCoatsOfArms, imgMap,
                        imagePath, coa.Url);
                    coa.Url = $"picture:{picture.Id}";
                }
            }

            dbContext.Posts.Add(post);
        }
        
        logger.LogInformation("Resolving links between posts...");
        foreach (var post in dbContext.Posts)
        {
            post.ContentMD = Regex.Replace(post.ContentMD,
                "\\([^)]+/([0-9]{4})-([0-9]{2})-([0-9]{2})-([^.]+)\\.ru\\.post\\.md\\)",
                match =>
                {
                    var date = new DateOnly(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value));
                    var name = match.Groups[4].Value;
                    var target = dbContext.Posts.FirstOrDefault(p => p.Name == name && p.Date == date);
                    if (target == null)
                    {
                        logger.LogWarning("Could not resolve post link: {link} in post {post}", match.Value, post);
                        return match.Value;
                    }

                    return $"(post:{target.Id})";
                });
        }

        var imageRedirsFilename = "image-redirs";
        logger.LogInformation("Writing out image redirs to {file}", imageRedirsFilename);
        await File.WriteAllLinesAsync(imageRedirsFilename, imgMap.Select(kv => $"{kv.Key.Replace(directory, "")} {kv.Value}"));

        await dbContext.SaveChangesAsync();
        logger.LogInformation("All done!");
    }


    private static Picture ProcessImage(ILogger logger, KoTiDbContext dbContext, PictureUpload pictureUpload, 
                                        PictureSet pictureSet, IDictionary<string, string> imgMap,
                                        string imagePath, string imageOriginalUrl)
    {
        // get real path of the image file and get SHA-1 hash of it
        logger.LogInformation("Processing image {image} -> {imagePath}", imageOriginalUrl, imagePath);
        using var sha1 = SHA1.Create(); 
        using var fileStream = File.OpenRead(imagePath);
        var hash = BitConverter.ToString(sha1.ComputeHash(fileStream)).Replace("-", "").ToLower();
        
        // is this image present in the database?
        var picture = dbContext.Pictures.FirstOrDefault(p => p.Hash == hash);
        if (picture != null)
        {
            // easy case then!
            logger.LogInformation("Hash: {hash} -> found picture #{id}", hash, picture.Id);
        }
        else
        {
            // need to upload 
            var uploadResult = Task.Run(async () =>
            {
                await using var fileStreamUpload = File.OpenRead(imagePath);
                return await pictureUpload.UploadAsync(fileStreamUpload, hash, Path.GetFileName(imagePath));
            }).GetAwaiter().GetResult();
            
            // retrieve metadata, we normally would do this on the frontend
            fileStream.Seek(0, SeekOrigin.Begin);
            var imageInfo = Image.Identify(fileStream);
            DateTime originalDateTime = DateTime.Now;
            var originalDateTimeString = (string?) imageInfo.Metadata.ExifProfile?.Values
                .FirstOrDefault(v => v.Tag == ExifTag.DateTimeOriginal)?.GetValue();
            if (originalDateTimeString != null && originalDateTimeString != "0000:00:00 00:00:00")
            {
                DateTime.TryParseExact(originalDateTimeString, "yyyy:MM:dd HH:mm:ss",
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out originalDateTime);
            }

            picture = new Picture
            {
                Hash = hash,
                Filename = Path.GetFileName(imagePath),
                Url = uploadResult.PictureUrl,
                DetailsUrl = uploadResult.DetailsUrl,
                ThumbnailUrl = uploadResult.ThumbnailUrl,
                Width = imageInfo.Width,
                Height = imageInfo.Height,
                Size = (int)new FileInfo(imagePath).Length,
                IsPrivate = true,
                Set = pictureSet,
                UploadedAt = DateTime.Now,
                Camera = (string?)imageInfo.Metadata.ExifProfile?.Values
                    .FirstOrDefault(v => v.Tag == ExifTag.Model)?.GetValue(),
                Lens = (string?)imageInfo.Metadata.ExifProfile?.Values
                    .FirstOrDefault(v => v.Tag == ExifTag.LensModel)?.GetValue(),
                PhotographedAt = originalDateTime
            };
            dbContext.Pictures.Add(picture);
            dbContext.SaveChanges();
            logger.LogInformation("Hash: {hash} -> created picture #{id} ({uploaded})", hash, picture.Id,
                uploadResult.ExistedInStorage ? "already uploaded to storage" : "uploaded now");
        }

        imgMap.TryAdd(imagePath, picture.Url);

        if (!picture.WebsiteSizesExist)
        {
            Task.Run(async () => await pictureUpload.EnsureWebsiteVersionsExist(picture))
                .GetAwaiter().GetResult();
            if (picture.Website1xUrl != null)
            {
                (int w, int h) = picture.GetDownsizedDimensions(Picture.WebsiteSize);
                logger.LogInformation("Rescaled and uploaded for web for 1x: {w}x{h}", w, h);
                imgMap.TryAdd(PictureUpload.GetFilenameWithSuffix(imagePath, ".1x", false), picture.Website1xUrl);
            }
            if (picture.Website2xUrl != null)
            {
                (int w, int h) = picture.GetDownsizedDimensions(Picture.WebsiteSize * 2);
                logger.LogInformation("Rescaled and uploaded for web for 2x: {w}x{h}", w, h);
                imgMap.TryAdd(PictureUpload.GetFilenameWithSuffix(imagePath, ".2x", false), picture.Website2xUrl);
            }
        }

        return picture;
    }
}
