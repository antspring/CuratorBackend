using System.Net;
using Application.Files;
using IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Files;

public class FileStorageTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly byte[] Content = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];

    private readonly IFileStorage storage = factory.Services.GetRequiredService<IFileStorage>();
    private readonly HttpClient anonymous = new();

    [Fact]
    public void GetPublicUrl_JoinsPublicBaseUrlAndKey()
    {
        var url = storage.GetPublicUrl("avatars/abc.png");

        Assert.Equal($"{factory.StorageUrl}/{ApiFactory.StorageBucket}/avatars/abc.png", url.ToString());
    }

    [Fact]
    public async Task UploadedObject_IsReadableAnonymouslyByPublicUrl()
    {
        var key = NewKey();

        await storage.UploadAsync(key, new MemoryStream(Content), "image/png", CancellationToken.None);

        var response = await anonymous.GetAsync(storage.GetPublicUrl(key));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Content, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Bucket_DoesNotAllowAnonymousListing()
    {
        await storage.UploadAsync(NewKey(), new MemoryStream(Content), "image/png", CancellationToken.None);

        var response = await anonymous.GetAsync($"{factory.StorageUrl}/{ApiFactory.StorageBucket}/");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeletedObject_IsNoLongerAvailable()
    {
        var key = NewKey();
        await storage.UploadAsync(key, new MemoryStream(Content), "image/png", CancellationToken.None);

        await storage.DeleteAsync(key, CancellationToken.None);

        var response = await anonymous.GetAsync(storage.GetPublicUrl(key));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static string NewKey() => $"avatars/{Guid.NewGuid():N}.png";
}
