using System.Buffers.Text;
using System.Security.Cryptography;
using Domain.Files;
using Microsoft.Extensions.Logging;

namespace Application.Files;

public sealed class FileService(
    IFileStorage storage,
    IFileMetadataStore metadata,
    TimeProvider timeProvider,
    ILogger<FileService> logger)
{
    private const int KeyRandomBytes = 16;

    public async Task<UploadFileResult> UploadAsync(UploadFileCommand command, CancellationToken cancellationToken)
    {
        if (command.Length <= 0)
        {
            return new UploadFileResult.Empty();
        }

        if (command.Length > StoredFile.MaxSizeBytes)
        {
            return new UploadFileResult.TooLarge();
        }

        using var buffer = new MemoryStream((int)command.Length);
        await command.Content.CopyToAsync(buffer, cancellationToken);

        if (buffer.Length > StoredFile.MaxSizeBytes)
        {
            return new UploadFileResult.TooLarge();
        }

        var format = ImageFormat.Detect(buffer.GetBuffer().AsSpan(0, (int)Math.Min(buffer.Length, ImageFormat.SignatureLength)));

        if (format is null)
        {
            return new UploadFileResult.UnsupportedType();
        }

        var file = new StoredFile(
            Guid.CreateVersion7(),
            command.OwnerId,
            command.Purpose,
            command.FileName ?? StoredFile.FallbackName,
            format.ContentType,
            buffer.Length,
            NewStorageKey(command.Purpose, format),
            timeProvider.GetUtcNow());

        buffer.Position = 0;
        await storage.UploadAsync(file.StorageKey, buffer, file.ContentType, cancellationToken);

        try
        {
            await metadata.AddAsync(file, cancellationToken);
        }
        catch
        {
            await DeleteOrphanAsync(file.StorageKey);
            throw;
        }

        return new UploadFileResult.Uploaded(FileView.From(file, storage.GetPublicUrl(file.StorageKey)));
    }

    public async Task<FileView?> FindOwnedAsync(Guid fileId, Guid ownerId, CancellationToken cancellationToken)
    {
        var file = await metadata.FindOwnedAsync(fileId, ownerId, cancellationToken);

        return file is null ? null : FileView.From(file, storage.GetPublicUrl(file.StorageKey));
    }

    private static string NewStorageKey(string purpose, ImageFormat format) =>
        $"{purpose}s/{Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(KeyRandomBytes))}{format.Extension}";

    private async Task DeleteOrphanAsync(string storageKey)
    {
        try
        {
            await storage.DeleteAsync(storageKey, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Не удалось удалить объект {StorageKey} после ошибки записи метаданных", storageKey);
        }
    }
}
