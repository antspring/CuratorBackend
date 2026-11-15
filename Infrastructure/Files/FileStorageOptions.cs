using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Files;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    [Required]
    [Url]
    public string ServiceUrl { get; init; } = null!;

    [Required]
    public string Region { get; init; } = null!;

    [Required]
    public string Bucket { get; init; } = null!;

    [Required]
    public string AccessKey { get; init; } = null!;

    [Required]
    public string SecretKey { get; init; } = null!;

    [Required]
    [Url]
    public string PublicBaseUrl { get; init; } = null!;

    public bool ForcePathStyle { get; init; }
}
