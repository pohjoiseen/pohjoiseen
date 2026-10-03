using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using Amazon.S3;
using Amazon.S3.Model;

namespace Tests.Infrastructure;

/// <summary>
/// In-memory stand-in for the S3 bucket and its public URL: <see cref="S3"/> is what PictureStorage writes to,
/// <see cref="HttpHandler"/> serves the same objects under <see cref="PublicUrl"/>.
/// </summary>
public class FakePictureStore
{
    public const string PublicUrl = "https://pictures.test/";

    public ConcurrentDictionary<string, (byte[] Content, string ContentType)> Objects { get; } = new();

    public IAmazonS3 S3 { get; }

    public HttpMessageHandler HttpHandler { get; }

    public FakePictureStore()
    {
        S3 = FakeS3Proxy.Create(this);
        HttpHandler = new Handler(this);
    }

    public void Put(string key, byte[] content, string contentType = "image/jpeg") => Objects[key] = (content, contentType);

    public byte[]? GetByUrl(string url) =>
        url.StartsWith(PublicUrl) && Objects.TryGetValue(url[PublicUrl.Length..], out var o) ? o.Content : null;

    private class Handler(FakePictureStore store) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            if (url.StartsWith(PublicUrl) && store.Objects.TryGetValue(url[PublicUrl.Length..], out var o))
            {
                var content = new ByteArrayContent(o.Content);
                content.Headers.ContentType = new(o.ContentType);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    /// <summary>
    /// Implements just the few IAmazonS3 methods PictureStorage uses.
    /// </summary>
    public class FakeS3Proxy : DispatchProxy
    {
        private FakePictureStore _store = null!;

        public static IAmazonS3 Create(FakePictureStore store)
        {
            var proxy = Create<IAmazonS3, FakeS3Proxy>();
            ((FakeS3Proxy)(object)proxy)._store = store;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            switch (method!.Name)
            {
                case nameof(IAmazonS3.ListObjectsAsync) when args![0] is string && args[1] is string prefix:
                    return Task.FromResult(new ListObjectsResponse
                    {
                        S3Objects = _store.Objects.Keys.Where(k => k.StartsWith(prefix)).Order()
                            .Select(k => new S3Object { Key = k }).ToList()
                    });
                case nameof(IAmazonS3.PutObjectAsync) when args![0] is PutObjectRequest request:
                    using (var memory = new MemoryStream())
                    {
                        request.InputStream.CopyTo(memory);
                        _store.Put(request.Key, memory.ToArray(), request.ContentType);
                    }
                    return Task.FromResult(new PutObjectResponse());
                case nameof(IAmazonS3.DeleteObjectAsync) when args![0] is string && args[1] is string key:
                    _store.Objects.TryRemove(key, out _);
                    return Task.FromResult(new DeleteObjectResponse());
                case nameof(IDisposable.Dispose):
                    return null;
                default:
                    throw new NotSupportedException($"FakeS3: {method.Name} not implemented");
            }
        }
    }
}
