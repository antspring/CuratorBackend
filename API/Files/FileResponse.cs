using Application.Files;

namespace API.Files;

public sealed record FileResponse(
    Guid Id,
    Guid OwnerId,
    string Purpose,
    string Name,
    string ContentType,
    long SizeBytes,
    Uri Url,
    DateTimeOffset CreatedAt)
{
    public static FileResponse From(FileView file) =>
        new(file.Id, file.OwnerId, file.Purpose, file.Name, file.ContentType, file.SizeBytes, file.Url, file.CreatedAt);
}
