using API.Authentication;
using API.Errors;
using Application.Mentees;
using Microsoft.AspNetCore.Mvc;

namespace API.Mentees;

[ApiController]
[Route("api/v1/me/mentee-profile")]
public sealed class MenteeProfileController(MenteeProfileService menteeProfileService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<MenteeProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MenteeProfileResponse>> Get(CancellationToken cancellationToken)
    {
        var profile = await menteeProfileService.FindAsync(User.GetUserId(), cancellationToken);

        return profile is null
            ? this.ProblemWithCode(StatusCodes.Status404NotFound, ErrorCodes.ResourceNotFound, "Анкета менти ещё не сохранена.")
            : Ok(MenteeProfileResponse.From(profile));
    }

    [HttpPut("personal")]
    [ProducesResponseType<MenteePersonalResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MenteePersonalResponse>> PutPersonal(MenteePersonalRequest request, CancellationToken cancellationToken)
    {
        var profile = await menteeProfileService.UpdatePersonalAsync(request.ToCommand(User.GetUserId()), cancellationToken);
        return profile is null
            ? this.ProblemWithCode(StatusCodes.Status404NotFound, ErrorCodes.ResourceNotFound, "Анкета менти ещё не сохранена.")
            : Ok(MenteePersonalResponse.From(profile));
    }

    [HttpPut("skills")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PutSkills(MenteeOwnedSkillsRequest request, CancellationToken cancellationToken)
    {
        var updated = await menteeProfileService.ReplaceOwnedSkillsAsync(User.GetUserId(), request.ToChoices(), cancellationToken);
        return updated
            ? NoContent()
            : this.ProblemWithCode(StatusCodes.Status404NotFound, ErrorCodes.ResourceNotFound, "Анкета менти ещё не сохранена.");
    }

    [HttpPut("goals")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PutGoals(MenteeGoalsRequest request, CancellationToken cancellationToken)
    {
        var updated = await menteeProfileService.ReplaceGoalsAsync(User.GetUserId(), request.ToChoices(), cancellationToken);
        return updated
            ? NoContent()
            : this.ProblemWithCode(StatusCodes.Status404NotFound, ErrorCodes.ResourceNotFound, "Анкета менти ещё не сохранена.");
    }

    [HttpPut("availability")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PutAvailability(MenteeAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var updated = await menteeProfileService.UpdateAvailabilityAsync(User.GetUserId(), request.ToAvailability(), cancellationToken);
        return updated
            ? NoContent()
            : this.ProblemWithCode(StatusCodes.Status404NotFound, ErrorCodes.ResourceNotFound, "Анкета менти ещё не сохранена.");
    }

    [HttpPut]
    [ProducesResponseType<MenteeProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MenteeProfileResponse>> Put(MenteeProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await menteeProfileService.SaveAsync(request.ToCommand(User.GetUserId()), cancellationToken);

        return result switch
        {
            SaveMenteeProfileResult.Saved saved => Ok(MenteeProfileResponse.From(saved.Profile)),
            SaveMenteeProfileResult.AccountNotFound =>
                this.ProblemWithCode(StatusCodes.Status401Unauthorized, ErrorCodes.Unauthenticated, "Учётная запись не найдена."),
            SaveMenteeProfileResult.RoleConflict =>
                this.ProblemWithCode(StatusCodes.Status409Conflict, ErrorCodes.RoleConflict, "У учётной записи уже есть роль ментора."),
            SaveMenteeProfileResult.Forbidden =>
                this.ProblemWithCode(StatusCodes.Status403Forbidden, ErrorCodes.Forbidden, "Анкета менти недоступна для этой роли."),
            _ => throw new InvalidOperationException($"Неизвестный результат сохранения анкеты: {result.GetType().Name}"),
        };
    }
}
