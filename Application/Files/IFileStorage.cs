namespace Application.Files;

public interface IFileStorage
{
    Task UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);

    Uri GetPublicUrl(string key);
}
