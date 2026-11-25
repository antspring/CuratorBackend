using System.Globalization;
using API.Errors;
using API.RateLimiting;
using Application.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Accounts;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
[Route("api/v1/accounts")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
public sealed class AccountsController(AccountService accountService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<VerificationTicket>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken) =>
        ToResponse(await accountService.RegisterAsync(new RegisterAccountCommand(request.Email, request.Password), cancellationToken));

    [HttpPost("{verificationId:guid}/confirm")]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status410Gone)]
    public async Task<IActionResult> Confirm(Guid verificationId, ConfirmRegistrationRequest request, CancellationToken cancellationToken) =>
        ToResponse(await accountService.ConfirmAsync(verificationId, request.Code, cancellationToken));

    [HttpPost("{verificationId:guid}/resend")]
    [ProducesResponseType<VerificationTicket>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status410Gone)]
    public async Task<IActionResult> Resend(Guid verificationId, CancellationToken cancellationToken) =>
        ToResponse(await accountService.ResendAsync(verificationId, cancellationToken));

    private IActionResult ToResponse(RegisterAccountResult result) => result switch
    {
        RegisterAccountResult.Pending pending => Accepted(pending.Ticket),
        RegisterAccountResult.Registered registered => Created("/api/v1/me", AccountResponse.From(registered.Account)),
        RegisterAccountResult.EmailAlreadyExists => this.ProblemWithCode(StatusCodes.Status409Conflict,
            ErrorCodes.AccountExists, "Учётная запись с таким email уже существует."),
        RegisterAccountResult.PasswordRejected rejected => this.FieldErrors("password", rejected.Errors),
        RegisterAccountResult.InvalidCode => this.ProblemWithCode(StatusCodes.Status400BadRequest,
            ErrorCodes.InvalidVerificationCode, "Неверный код подтверждения."),
        RegisterAccountResult.Unavailable => this.ProblemWithCode(StatusCodes.Status410Gone,
            ErrorCodes.VerificationUnavailable, "Код больше недоступен. Запросите новый код или начните регистрацию заново."),
        RegisterAccountResult.TooSoon limited => TooSoon(limited.RetryAfter),
        _ => throw new InvalidOperationException($"Неизвестный результат регистрации: {result.GetType().Name}"),
    };

    private ObjectResult TooSoon(TimeSpan delay)
    {
        Response.Headers.RetryAfter = Math.Ceiling(delay.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        return this.ProblemWithCode(StatusCodes.Status429TooManyRequests, ErrorCodes.RateLimitExceeded,
            "Подождите перед повторной отправкой письма.");
    }
}
