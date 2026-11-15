using Domain.Common;

namespace Domain.Files;

public sealed class StoredFile
{
    public const long MaxSizeBytes = 10 * 1024 * 1024;
    public const int NameMaxLength = 255;
    public const int StorageKeyMaxLength = 200;
    public const int ContentTypeMaxLength = 100;
    public const string FallbackName = "file";

    private StoredFile()
    {
    }

    public StoredFile(
        Guid id,
        Guid ownerId,
        string purpose,
        string name,
        string contentType,
        long sizeBytes,
        string storageKey,
        DateTimeOffset now)
    {
        if (ownerId == Guid.Empty)
        {
            throw new DomainException(nameof(OwnerId), "Владелец файла не задан.");
        }

        if (!FilePurposes.All.Contains(purpose))
        {
            throw new DomainException(nameof(Purpose), $"Допустимое назначение файла: {string.Join(", ", FilePurposes.All)}.");
        }

        if (sizeBytes is <= 0 or > MaxSizeBytes)
        {
            throw new DomainException(nameof(SizeBytes), "Размер файла — от 1 байта до 10 MiB.");
        }

        if (string.IsNullOrWhiteSpace(contentType) || contentType.Length > ContentTypeMaxLength)
        {
            throw new DomainException(nameof(ContentType), "Тип содержимого не задан.");
        }

        if (string.IsNullOrWhiteSpace(storageKey) || storageKey.Length > StorageKeyMaxLength)
        {
            throw new DomainException(nameof(StorageKey), "Ключ объекта не задан.");
        }

        Id = id;
        OwnerId = ownerId;
        Purpose = purpose;
        Name = SanitizeName(name);
        ContentType = contentType;
        SizeBytes = sizeBytes;
        StorageKey = storageKey;
        CreatedAt = now.ToUniversalTime();
    }

    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public string Purpose { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string ContentType { get; private set; } = null!;

    public long SizeBytes { get; private set; }

    public string StorageKey { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public static string SanitizeName(string? name)
    {
        var lastSegment = (name ?? string.Empty).Split('/', '\\')[^1];
        var printable = new string(lastSegment.Where(character => !char.IsControl(character)).ToArray()).Trim();

        if (printable.Length == 0 || printable is "." or "..")
        {
            return FallbackName;
        }

        return printable.Length <= NameMaxLength ? printable : printable[..NameMaxLength];
    }
}
