using API.Errors;
using Application.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Sessions;

[ApiController]
[Route("api/v1/session")]
public sealed class SessionController(SessionService sessionService) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
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

    private ActionResult NoStore(ActionResult result)
    {
        Response.Headers.CacheControl = "no-store";
        return result;
    }
}
