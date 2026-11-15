using Domain.Files;

namespace Application.Files;

public sealed record UploadFileCommand(Guid OwnerId, string Purpose, string? FileName, long Length, Stream Content);

public sealed record FileView(
    Guid Id,
    Guid OwnerId,
    string Purpose,
    string Name,
    string ContentType,
    long SizeBytes,
    Uri Url,
    DateTimeOffset CreatedAt)
{
    public static FileView From(StoredFile file, Uri url) =>
        new(file.Id, file.OwnerId, file.Purpose, file.Name, file.ContentType, file.SizeBytes, url, file.CreatedAt);
}

public abstract record UploadFileResult
{
    public sealed record Uploaded(FileView File) : UploadFileResult;

    public sealed record Empty : UploadFileResult;

    public sealed record TooLarge : UploadFileResult;

    public sealed record UnsupportedType : UploadFileResult;
}
