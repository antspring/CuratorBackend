using API.Errors;
using Application.Mentors;
using Microsoft.AspNetCore.Mvc;

namespace API.Mentors;

[ApiController]
[Route("api/v1/mentors")]
public sealed class MentorsController(MentorCardService mentorCardService) : ControllerBase
{
    [HttpGet("{mentorId:guid}")]
    [ProducesResponseType<MentorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MentorResponse>> Get(Guid mentorId, CancellationToken cancellationToken)
    {
        var card = await mentorCardService.FindAsync(mentorId, cancellationToken);

        return card is null
            ? this.ProblemWithCode(StatusCodes.Status404NotFound, ErrorCodes.ResourceNotFound, "Наставник не найден.")
            : Ok(MentorResponse.From(card));
    }
}
