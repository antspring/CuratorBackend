using Domain.Mentorships;

namespace API.Mentorships;

public sealed record MentorshipRequestResponse(
    Guid Id,
    Guid MenteeId,
    Guid MentorId,
    int DurationMonths,
    string? Message,
    MentorshipRequestStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DecidedAt,
    Guid? MentorshipId)
{
    public Guid? PreviousMentorshipId => null;

    public string? ReplacementReasonCode => null;

    public string? DecisionReason => null;

    public static MentorshipRequestResponse From(MentorshipRequest request, Guid? mentorshipId) =>
        new(request.Id, request.MenteeId, request.MentorId, request.DurationMonths, request.Message,
            request.Status, request.CreatedAt, request.DecidedAt, mentorshipId);
}

public sealed record MentorshipResponse(
    Guid Id,
    Guid RequestId,
    Guid MenteeId,
    Guid MentorId,
    DateTimeOffset StartsAt,
    int DurationMonths,
    DateTimeOffset PlannedEndsAt,
    MentorshipStatus Status,
    DateTimeOffset? EndedAt,
    MentorshipEndReasonCode? EndReasonCode,
    string? EndReason)
{
    public Guid? PreviousMentorshipId => null;

    public static MentorshipResponse From(Mentorship mentorship) =>
        new(mentorship.Id, mentorship.RequestId, mentorship.MenteeId, mentorship.MentorId, mentorship.StartsAt,
            mentorship.DurationMonths, mentorship.PlannedEndsAt, mentorship.Status, mentorship.EndedAt,
            mentorship.EndReasonCode, mentorship.EndReason);
}

public sealed record MentorshipAcceptanceResponse(MentorshipRequestResponse Request, MentorshipResponse Mentorship);
