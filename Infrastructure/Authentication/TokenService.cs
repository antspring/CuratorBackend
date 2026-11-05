using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Accounts;
using Application.Sessions;
using Infrastructure.Persistence;
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

    public async Task<TokenPair> IssueAsync(Account account, CancellationToken cancellationToken)
    {
        var jwt = jwtOptions.Value;
        var now = timeProvider.GetUtcNow();
        var accessTokenExpiresAt = now.Add(jwt.AccessTokenLifetime);
        var refreshTokenExpiresAt = now.Add(jwt.RefreshTokenLifetime);

        var refreshToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenBytes));

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = account.Id,
            TokenHash = Hash(refreshToken),
            CreatedAt = now,
            ExpiresAt = refreshTokenExpiresAt,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new TokenPair(
            CreateAccessToken(account, jwt, now, accessTokenExpiresAt),
            accessTokenExpiresAt,
            refreshToken,
            refreshTokenExpiresAt);
    }

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
