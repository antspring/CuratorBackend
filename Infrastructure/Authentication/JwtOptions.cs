using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = null!;

    [Required]
    public string Audience { get; init; } = null!;

    [Required]
    [MinLength(32, ErrorMessage = "Jwt:SigningKey должен быть не короче 32 символов.")]
    public string SigningKey { get; init; } = null!;

    [Range(typeof(TimeSpan), "00:01:00", "01:00:00")]
    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(15);

    [Range(typeof(TimeSpan), "1.00:00:00", "90.00:00:00")]
    public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(14);
}
