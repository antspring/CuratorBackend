using System.Net;
using System.Net.Http.Headers;
using Domain.Files;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Files;

public sealed record FileDto(
    Guid Id,
    Guid OwnerId,
    string Purpose,
    string Name,
    string ContentType,
    long SizeBytes,
    string Url,
    DateTimeOffset CreatedAt);

public class FilesEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];
    private static readonly byte[] Pdf = "%PDF-1.7\n"u8.ToArray();

    private readonly HttpClient client = factory.CreateClient();
    private readonly HttpClient anonymous = new();
    private TokenPairDto tokens = null!;

    public async Task InitializeAsync()
    {
        tokens = await client.RegisterAndLoginAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Upload_Png_ReturnsCreatedFileWithPublicUrl()
    {
        var response = await UploadAsync(Png, "avatar.png");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var file = await response.ReadAsync<FileDto>();
        Assert.Equal($"/api/v1/files/{file.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(tokens.Account.Id, file.OwnerId);
        Assert.Equal("avatar", file.Purpose);
        Assert.Equal("avatar.png", file.Name);
        Assert.Equal("image/png", file.ContentType);
        Assert.Equal(Png.Length, file.SizeBytes);
        Assert.StartsWith($"{factory.StorageUrl}/{ApiFactory.StorageBucket}/avatars/", file.Url);
        Assert.EndsWith(".png", file.Url);
        Assert.DoesNotContain("avatar.png", file.Url);
        Assert.DoesNotContain(file.Id.ToString(), file.Url);

        var download = await anonymous.GetAsync(file.Url);
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal(Png, await download.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Upload_Jpeg_DetectsTypeByContentNotByName()
    {
        var response = await UploadAsync(Jpeg, "photo.png", "image/png");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var file = await response.ReadAsync<FileDto>();
        Assert.Equal("image/jpeg", file.ContentType);
        Assert.EndsWith(".jpg", file.Url);
    }

    [Fact]
    public async Task Upload_SanitizesFileName()
    {
        var response = await UploadAsync(Png, "../../secret/avatar.png");

        Assert.Equal("avatar.png", (await response.ReadAsync<FileDto>()).Name);
    }

    [Fact]
    public async Task Upload_PdfDisguisedAsPng_ReturnsUnsupportedMediaType()
    {
        var response = await UploadAsync(Pdf, "avatar.png", "image/png");

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.Equal("unsupported_media_type", (await response.ReadAsync<ProblemDto>()).Code);
    }

    [Fact]
    public async Task Upload_LargerThanLimit_ReturnsFileTooLarge()
    {
        var content = new byte[StoredFile.MaxSizeBytes + 1];
        Png.CopyTo(content, 0);

        var response = await UploadAsync(content, "big.png");

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal("file_too_large", (await response.ReadAsync<ProblemDto>()).Code);
    }

    [Fact]
    public async Task Upload_RequestBodyOverLimit_ReturnsFileTooLargeBeforeReadingForm()
    {
        var response = await UploadAsync(new byte[StoredFile.MaxSizeBytes + 1024 * 1024], "huge.png");

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal("file_too_large", (await response.ReadAsync<ProblemDto>()).Code);
    }

    [Fact]
    public async Task Upload_EmptyFile_ReturnsValidationError()
    {
        var response = await UploadAsync([], "empty.png");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.ReadAsync<ProblemDto>()).Errors?.ContainsKey("file"));
    }

    [Fact]
    public async Task Upload_UnsupportedPurpose_ReturnsValidationError()
    {
        var response = await UploadAsync(Png, "avatar.png", purpose: "message_attachment");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.ReadAsync<ProblemDto>()).Errors?.ContainsKey("purpose"));
    }

    [Fact]
    public async Task Upload_WithoutToken_ReturnsUnauthorized()
    {
        var response = await factory.CreateClient().PostAsync("/api/v1/files", Form(Png, "avatar.png"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Upload_JsonBody_ReturnsUnsupportedMediaType()
    {
        var response = await client.PostAsync("/api/v1/files", new StringContent("{}", null, "application/json"));

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task Get_OwnFile_ReturnsMetadata()
    {
        var uploaded = await (await UploadAsync(Png, "avatar.png")).ReadAsync<FileDto>();

        var response = await client.GetAsync($"/api/v1/files/{uploaded.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(uploaded, await response.ReadAsync<FileDto>());
    }

    [Fact]
    public async Task Get_OtherUsersFile_ReturnsNotFound()
    {
        var uploaded = await (await UploadAsync(Png, "avatar.png")).ReadAsync<FileDto>();
        var stranger = factory.CreateClient();
        var strangerTokens = await stranger.RegisterAndLoginAsync();
        stranger.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", strangerTokens.AccessToken);

        var response = await stranger.GetAsync($"/api/v1/files/{uploaded.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("resource_not_found", (await response.ReadAsync<ProblemDto>()).Code);
    }

    [Fact]
    public async Task Get_UnknownFile_ReturnsNotFound()
    {
        var response = await client.GetAsync($"/api/v1/files/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private Task<HttpResponseMessage> UploadAsync(
        byte[] content,
        string fileName,
        string declaredType = "application/octet-stream",
        string purpose = FilePurposes.Avatar) =>
        client.PostAsync("/api/v1/files", Form(content, fileName, declaredType, purpose));

    private static MultipartFormDataContent Form(
        byte[] content,
        string fileName,
        string declaredType = "application/octet-stream",
        string purpose = FilePurposes.Avatar)
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(declaredType);

        return new MultipartFormDataContent
        {
            { new StringContent(purpose), "purpose" },
            { file, "file", fileName },
        };
    }
}
