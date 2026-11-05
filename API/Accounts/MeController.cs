using API.Authentication;
using API.Errors;
using Application.Accounts;
using Microsoft.AspNetCore.Mvc;

namespace API.Accounts;

[ApiController]
[Route("api/v1/me")]
public sealed class MeController(AccountService accountService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AccountResponse>> Get(CancellationToken cancellationToken)
    {
        var account = await accountService.FindAsync(User.GetUserId(), cancellationToken);

        return account is null
            ? this.ProblemWithCode(StatusCodes.Status401Unauthorized, ErrorCodes.Unauthenticated, "Учётная запись не найдена.")
            : Ok(AccountResponse.From(account));
    }
}
