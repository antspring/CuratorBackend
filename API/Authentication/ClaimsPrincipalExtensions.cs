using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace API.Authentication;

internal static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)
            ? userId
            : throw new InvalidOperationException("В токене нет корректного идентификатора пользователя.");
}
