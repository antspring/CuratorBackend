namespace Infrastructure.Authentication;

public sealed class RefreshToken
{
    public const int TokenHashLength = 64;

    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public string TokenHash { get; init; } = null!;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset ExpiresAt { get; init; }

    public DateTimeOffset? RevokedAt { get; set; }

    public Guid? ReplacedByTokenId { get; set; }
}
