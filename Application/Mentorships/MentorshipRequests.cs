using Domain.Mentorships;

namespace Application.Mentorships;

public enum MentorshipRequestDirection
{
    Outgoing,
    Incoming,
}

public sealed record MentorshipRequestSnapshot(MentorshipRequest Request, Guid? MentorshipId);

public sealed record MentorshipRequestPosition(DateTimeOffset CreatedAt, Guid Id);

public sealed record MentorshipRequestPage(IReadOnlyList<MentorshipRequestSnapshot> Items, string? NextCursor);

public abstract record SendMentorshipRequestResult
{
    public sealed record Sent(MentorshipRequest Request) : SendMentorshipRequestResult;

    public sealed record ProfileIncomplete : SendMentorshipRequestResult;

    public sealed record MentorNotFound : SendMentorshipRequestResult;

    public sealed record RequestExists : SendMentorshipRequestResult;

    public sealed record ActiveMentorshipExists : SendMentorshipRequestResult;
}

public abstract record AcceptMentorshipRequestResult
{
    public sealed record Accepted(MentorshipRequest Request, Mentorship Mentorship) : AcceptMentorshipRequestResult;

    public sealed record NotFound : AcceptMentorshipRequestResult;

    public sealed record InvalidTransition : AcceptMentorshipRequestResult;

    public sealed record ActiveMentorshipExists : AcceptMentorshipRequestResult;
}
