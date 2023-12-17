using Amazon.S3;
using Amazon.S3.Model;

namespace KoTi;

public class PictureStorage
{
    private readonly IAmazonS3 _amazonS3;
    private readonly string _bucketName;
    public string PublicUrl { get; private set; }
    
    public PictureStorage(IAmazonS3 amazonS3, string bucketName, string publicUrl)
    {
        _amazonS3 = amazonS3;
        _bucketName = bucketName;
        PublicUrl = publicUrl;
    }

    public async Task<string?> CheckPictureAlreadyUploadedAsync(string hash)
    {
        var objects = await _amazonS3.ListObjectsAsync(_bucketName, hash + '/');
        if (objects.S3Objects.Count > 0)
        {
            var existingObject = objects.S3Objects[0];
            return existingObject.Key;
        }

        return null;
    }

    public async Task UploadPictureAsync(string name, Stream content)
    {
        await _amazonS3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = name,
            InputStream = content,
            ContentType = "image/jpeg",
            CannedACL = S3CannedACL.PublicRead
        });
    }
}