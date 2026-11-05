using API.Errors;
using API.RateLimiting;
using Application.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Sessions;

[ApiController]
[Route("api/v1/session")]
public sealed class SessionController(SessionService sessionService) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    [ProducesResponseType<TokenPairResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenPairResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await sessionService.LoginAsync(new LoginCommand(request.Email, request.Password), cancellationToken);

        return result switch
        {
            LoginResult.Succeeded succeeded => NoStore(Ok(TokenPairResponse.From(succeeded.Account, succeeded.Tokens))),
            LoginResult.InvalidCredentials =>
                this.ProblemWithCode(StatusCodes.Status401Unauthorized, ErrorCodes.InvalidCredentials, "Неверный email или пароль."),
            _ => throw new InvalidOperationException($"Неизвестный результат входа: {result.GetType().Name}"),
        };
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    [ProducesResponseType<TokenPairResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenPairResponse>> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        var result = await sessionService.RefreshAsync(request.RefreshToken, cancellationToken);

        return result switch
        {
            RefreshResult.Succeeded succeeded => NoStore(Ok(TokenPairResponse.From(succeeded.Account, succeeded.Tokens))),
            RefreshResult.InvalidRefreshToken =>
                this.ProblemWithCode(StatusCodes.Status401Unauthorized, ErrorCodes.InvalidRefreshToken, "Refresh-токен недействителен. Войдите заново."),
            _ => throw new InvalidOperationException($"Неизвестный результат обновления: {result.GetType().Name}"),
        };
    }

    [HttpDelete]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken cancellationToken)
    {
        await sessionService.LogoutAsync(request.RefreshToken, cancellationToken);

        return NoContent();
    }

    private ActionResult NoStore(ActionResult result)
    {
        Response.Headers.CacheControl = "no-store";
        return result;
    }
}
