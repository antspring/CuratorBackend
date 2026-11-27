using System.ComponentModel.DataAnnotations;
using API.Authentication;
using API.Common;
using API.Errors;
using Application.Mentorships;
using Domain.Mentorships;
using Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Mentorships;

[ApiController]
[Route("api/v1/mentorship-requests")]
public sealed class MentorshipRequestsController(MentorshipService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<MentorshipRequestResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MentorshipRequestResponse>> Send(
        SendMentorshipRequest input, CancellationToken cancellationToken)
    {
        if (!User.IsInRole(Roles.Mentee) && Roles.All.Any(User.IsInRole))
        {
            return this.ProblemWithCode(StatusCodes.Status403Forbidden, ErrorCodes.Forbidden, "Отправить заявку может только менти.");
        }

        var result = await service.SendAsync(User.GetUserId(), input.MentorId!.Value, input.DurationMonths!.Value,
            input.Message, cancellationToken);

        return result switch
        {
            SendMentorshipRequestResult.Sent sent => Created(Url.Action(nameof(Get), new { requestId = sent.Request.Id })!,
                MentorshipRequestResponse.From(sent.Request, null)),
            SendMentorshipRequestResult.ProfileIncomplete => this.ProblemWithCode(StatusCodes.Status409Conflict,
                ErrorCodes.ProfileIncomplete, "Сначала заполните анкету менти."),
            SendMentorshipRequestResult.MentorNotFound => this.ProblemWithCode(StatusCodes.Status404NotFound,
                ErrorCodes.ResourceNotFound, "Наставник не найден."),
            SendMentorshipRequestResult.RequestExists => this.ProblemWithCode(StatusCodes.Status409Conflict,
                ErrorCodes.RequestExists, "Наставнику уже отправлена заявка."),
            SendMentorshipRequestResult.ActiveMentorshipExists => ActiveMentorshipConflict(),
            _ => throw new InvalidOperationException($"Неизвестный результат отправки заявки: {result.GetType().Name}"),
        };
    }

    [HttpGet]
    [ProducesResponseType<PageResponse<MentorshipRequestResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PageResponse<MentorshipRequestResponse>>> List(
        [FromQuery] MentorshipRequestDirection direction = MentorshipRequestDirection.Outgoing,
        [FromQuery] MentorshipRequestStatus? status = null,
        [FromQuery] [Range(PageResponse.MinLimit, PageResponse.MaxLimit)] int limit = PageResponse.DefaultLimit,
        [FromQuery] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var role = direction == MentorshipRequestDirection.Incoming ? Roles.Mentor : Roles.Mentee;
        if (!User.IsInRole(role))
        {
            return this.ProblemWithCode(StatusCodes.Status403Forbidden, ErrorCodes.Forbidden,
                "Заявки этого направления недоступны для вашей роли.");
        }

        var page = await service.ListRequestsAsync(User.GetUserId(), direction, status, limit, cursor, cancellationToken);
        return page is null
            ? this.ProblemWithCode(StatusCodes.Status400BadRequest, ErrorCodes.InvalidCursor, "Курсор повреждён или относится к другим фильтрам.")
            : Ok(new PageResponse<MentorshipRequestResponse>(page.Items
                .Select(item => MentorshipRequestResponse.From(item.Request, item.MentorshipId)).ToArray(), page.NextCursor));
    }

    [HttpGet("{requestId:guid}")]
    [ProducesResponseType<MentorshipRequestResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MentorshipRequestResponse>> Get(Guid requestId, CancellationToken cancellationToken)
    {
        var item = await service.FindRequestAsync(requestId, User.GetUserId(), cancellationToken);
        return item is null ? RequestNotFound() : Ok(MentorshipRequestResponse.From(item.Request, item.MentorshipId));
    }

    [Authorize(Roles = Roles.Mentor)]
    [HttpPost("{requestId:guid}/accept")]
    [ProducesResponseType<MentorshipAcceptanceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MentorshipAcceptanceResponse>> Accept(Guid requestId, CancellationToken cancellationToken)
    {
        var result = await service.AcceptAsync(requestId, User.GetUserId(), cancellationToken);
        return result switch
        {
            AcceptMentorshipRequestResult.Accepted accepted => Ok(new MentorshipAcceptanceResponse(
                MentorshipRequestResponse.From(accepted.Request, accepted.Mentorship.Id), MentorshipResponse.From(accepted.Mentorship))),
            AcceptMentorshipRequestResult.NotFound => RequestNotFound(),
            AcceptMentorshipRequestResult.InvalidTransition => this.ProblemWithCode(StatusCodes.Status409Conflict,
                ErrorCodes.InvalidTransition, "Заявка уже закрыта."),
            AcceptMentorshipRequestResult.ActiveMentorshipExists => ActiveMentorshipConflict(),
            _ => throw new InvalidOperationException($"Неизвестный результат принятия заявки: {result.GetType().Name}"),
        };
    }

    private ObjectResult RequestNotFound() => this.ProblemWithCode(StatusCodes.Status404NotFound,
        ErrorCodes.ResourceNotFound, "Заявка не найдена.");

    private ObjectResult ActiveMentorshipConflict() => this.ProblemWithCode(StatusCodes.Status409Conflict,
        ErrorCodes.ActiveMentorshipExists, "У менти и наставника уже есть активная пара.");
}
