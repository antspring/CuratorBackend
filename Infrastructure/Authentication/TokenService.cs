using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Accounts;
using Application.Sessions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Authentication;

internal sealed class TokenService(
    AppDbContext dbContext,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider timeProvider) : ITokenService
{
    private const int RefreshTokenBytes = 32;

    private readonly JsonWebTokenHandler tokenHandler = new();

    public async Task<TokenPair> IssueAsync(Account account, CancellationToken cancellationToken, Guid? replacesTokenId = null)
    {
        var jwt = jwtOptions.Value;
        var now = timeProvider.GetUtcNow();
        var accessTokenExpiresAt = now.Add(jwt.AccessTokenLifetime);
        var refreshTokenExpiresAt = now.Add(jwt.RefreshTokenLifetime);

        var refreshToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenBytes));
        var refreshTokenId = Guid.CreateVersion7();

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = refreshTokenId,
            UserId = account.Id,
            TokenHash = Hash(refreshToken),
            CreatedAt = now,
            ExpiresAt = refreshTokenExpiresAt,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        if (replacesTokenId is { } previousTokenId)
        {
            await dbContext.RefreshTokens
                .Where(token => token.Id == previousTokenId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.ReplacedByTokenId, refreshTokenId), cancellationToken);
        }

        return new TokenPair(
            CreateAccessToken(account, jwt, now, accessTokenExpiresAt),
            accessTokenExpiresAt,
            refreshToken,
            refreshTokenExpiresAt);
    }

    public async Task<ConsumedRefreshToken?> ConsumeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = Hash(refreshToken);
        var now = timeProvider.GetUtcNow();

        var stored = await dbContext.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (stored is null || stored.ExpiresAt <= now)
        {
            return null;
        }

        var revoked = stored.RevokedAt is null && await dbContext.RefreshTokens
            .Where(token => token.Id == stored.Id && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, now), cancellationToken) == 1;

        if (!revoked)
        {
            await RevokeAllForUserAsync(stored.UserId, now, cancellationToken);
            return null;
        }

        return new ConsumedRefreshToken(stored.Id, stored.UserId);
    }

    public Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = Hash(refreshToken);
        var now = timeProvider.GetUtcNow();

        return dbContext.RefreshTokens
            .Where(token => token.TokenHash == tokenHash && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, now), cancellationToken);
    }

    private Task RevokeAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, now), cancellationToken);

    internal static string Hash(string refreshToken) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

    private string CreateAccessToken(Account account, JwtOptions jwt, DateTimeOffset now, DateTimeOffset expiresAt)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, account.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(account.Roles.Select(role => new Claim("role", role)));

        return tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                SecurityAlgorithms.HmacSha256),
        });
    }
}
