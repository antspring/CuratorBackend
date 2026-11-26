using API.Authentication;
using API.Errors;
using Application.Mentors;
using Microsoft.AspNetCore.Mvc;

namespace API.Mentors;

[ApiController]
[Route("api/v1/me/mentor-profile")]
public sealed class MentorProfileController(MentorProfileService mentorProfileService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<MentorProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MentorProfileResponse>> Get(CancellationToken cancellationToken)
    {
        var profile = await mentorProfileService.FindAsync(User.GetUserId(), cancellationToken);

        return profile is null
            ? this.ProblemWithCode(StatusCodes.Status404NotFound, ErrorCodes.ResourceNotFound, "Анкета ментора ещё не сохранена.")
            : Ok(MentorProfileResponse.From(profile));
    }

    [HttpPut("work-experience")]
    [ProducesResponseType<MentorWorkExperienceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MentorWorkExperienceResponse>> PutWorkExperience(
        MentorWorkExperienceRequest request, CancellationToken cancellationToken)
    {
        var saved = await mentorProfileService.ReplaceWorkplacesAsync(User.GetUserId(), request.ToChoices(), cancellationToken);
        return saved is null
            ? this.ProblemWithCode(StatusCodes.Status404NotFound, ErrorCodes.ResourceNotFound, "Анкета ментора ещё не сохранена.")
            : Ok(new MentorWorkExperienceResponse(saved));
    }

    [HttpPut]
    [ProducesResponseType<MentorProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MentorProfileResponse>> Put(MentorProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await mentorProfileService.SaveAsync(request.ToCommand(User.GetUserId()), cancellationToken);

        return result switch
        {
            SaveMentorProfileResult.Saved saved => Ok(MentorProfileResponse.From(saved.Profile)),
            SaveMentorProfileResult.AccountNotFound =>
                this.ProblemWithCode(StatusCodes.Status401Unauthorized, ErrorCodes.Unauthenticated, "Учётная запись не найдена."),
            SaveMentorProfileResult.RoleConflict =>
                this.ProblemWithCode(StatusCodes.Status409Conflict, ErrorCodes.RoleConflict, "У учётной записи уже есть роль менти."),
            SaveMentorProfileResult.Forbidden =>
                this.ProblemWithCode(StatusCodes.Status403Forbidden, ErrorCodes.Forbidden, "Анкета ментора недоступна для этой роли."),
            _ => throw new InvalidOperationException($"Неизвестный результат сохранения анкеты: {result.GetType().Name}"),
        };
    }
}
