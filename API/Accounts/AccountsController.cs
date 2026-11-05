using API.Errors;
using Application.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Accounts;

[ApiController]
[Route("api/v1/accounts")]
public sealed class AccountsController(AccountService accountService) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AccountResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterAccountCommand(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.Timezone,
            request.Roles);

        var result = await accountService.RegisterAsync(command, cancellationToken);

        return result switch
        {
            RegisterAccountResult.Registered registered =>
                Created("/api/v1/me", AccountResponse.From(registered.Account)),
            RegisterAccountResult.EmailAlreadyExists =>
                this.ProblemWithCode(StatusCodes.Status409Conflict, ErrorCodes.AccountExists, "Учётная запись с таким email уже существует."),
            RegisterAccountResult.PasswordRejected rejected =>
                this.FieldErrors("password", rejected.Errors),
            _ => throw new InvalidOperationException($"Неизвестный результат регистрации: {result.GetType().Name}"),
        };
    }
}
