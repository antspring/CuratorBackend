using API.Authentication;
using API.Errors;
using Application.Mentorships;
using Microsoft.AspNetCore.Mvc;

namespace API.Mentorships;

[ApiController]
[Route("api/v1/mentorships")]
public sealed class MentorshipsController(MentorshipService service) : ControllerBase
{
    [HttpGet("{mentorshipId:guid}")]
    [ProducesResponseType<MentorshipResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MentorshipResponse>> Get(Guid mentorshipId, CancellationToken cancellationToken)
    {
        var mentorship = await service.FindMentorshipAsync(mentorshipId, User.GetUserId(), cancellationToken);
        return mentorship is null
            ? this.ProblemWithCode(StatusCodes.Status404NotFound, ErrorCodes.ResourceNotFound, "Наставническая пара не найдена.")
            : Ok(MentorshipResponse.From(mentorship));
    }
}
