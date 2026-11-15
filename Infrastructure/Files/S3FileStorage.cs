using Amazon.S3;
using Amazon.S3.Model;
using Application.Files;
using Microsoft.Extensions.Options;

namespace Infrastructure.Files;

internal sealed class S3FileStorage(IAmazonS3 s3, IOptions<FileStorageOptions> options) : IFileStorage
{
    private const string ImmutableCacheControl = "public, max-age=31536000, immutable";

    private readonly FileStorageOptions settings = options.Value;

    public async Task UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
    {
        var request = new PutObjectRequest
        {
            BucketName = settings.Bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
        };
        request.Headers.CacheControl = ImmutableCacheControl;

        await s3.PutObjectAsync(request, cancellationToken);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken) =>
        await s3.DeleteObjectAsync(settings.Bucket, key, cancellationToken);

    public Uri GetPublicUrl(string key) => new($"{settings.PublicBaseUrl.TrimEnd('/')}/{key}");
}
