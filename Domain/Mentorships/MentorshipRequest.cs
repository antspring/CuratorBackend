using Domain.Common;

namespace Domain.Mentorships;

public enum MentorshipRequestStatus
{
    Pending,
    Accepted,
    Rejected,
    Cancelled,
}

public sealed class MentorshipRequest
{
    public const int MessageMaxLength = 2000;

    private MentorshipRequest()
    {
    }

    public MentorshipRequest(Guid menteeId, Guid mentorId, int durationMonths, string? message, DateTimeOffset now)
    {
        MenteeId = Require.Id(menteeId, nameof(MenteeId));
        MentorId = Require.Id(mentorId, nameof(MentorId));

        if (MenteeId == MentorId)
        {
            throw new DomainException(nameof(MentorId), "Нельзя отправить заявку себе.");
        }

        Id = Guid.CreateVersion7();
        DurationMonths = Mentorship.RequireDurationMonths(durationMonths);
        Message = string.IsNullOrWhiteSpace(message) ? null : Require.Text(message, 1, MessageMaxLength, nameof(Message));
        CreatedAt = now.ToUniversalTime();
    }

    public Guid Id { get; private set; }

    public Guid MenteeId { get; private set; }

    public Guid MentorId { get; private set; }

    public int DurationMonths { get; private set; }

    public string? Message { get; private set; }

    public MentorshipRequestStatus Status { get; private set; } = MentorshipRequestStatus.Pending;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? DecidedAt { get; private set; }

    public Mentorship? Accept(DateTimeOffset now)
    {
        if (Status != MentorshipRequestStatus.Pending)
        {
            return null;
        }

        var mentorship = new Mentorship(Id, MenteeId, MentorId, DurationMonths, now);
        Status = MentorshipRequestStatus.Accepted;
        DecidedAt = mentorship.StartsAt;
        return mentorship;
    }
}
