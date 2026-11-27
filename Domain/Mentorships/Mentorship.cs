using Domain.Common;

namespace Domain.Mentorships;

public enum MentorshipStatus
{
    Active,
    Completed,
    Terminated,
}

public enum MentorshipEndReasonCode
{
    Unsatisfactory,
    GoalsChanged,
    Other,
}

public sealed class Mentorship
{
    public const int EndReasonMaxLength = 4000;

    private Mentorship()
    {
    }

    public Mentorship(Guid requestId, Guid menteeId, Guid mentorId, int durationMonths, DateTimeOffset startsAt)
    {
        RequestId = Require.Id(requestId, nameof(RequestId));
        MenteeId = Require.Id(menteeId, nameof(MenteeId));
        MentorId = Require.Id(mentorId, nameof(MentorId));

        if (MenteeId == MentorId)
        {
            throw new DomainException(nameof(MentorId), "Участниками пары должны быть разные пользователи.");
        }

        Id = Guid.CreateVersion7();
        StartsAt = startsAt.ToUniversalTime();
        DurationMonths = RequireDurationMonths(durationMonths);
        PlannedEndsAt = StartsAt.AddMonths(DurationMonths);
    }

    public Guid Id { get; private set; }

    public Guid RequestId { get; private set; }

    public Guid MenteeId { get; private set; }

    public Guid MentorId { get; private set; }

    public DateTimeOffset StartsAt { get; private set; }

    public int DurationMonths { get; private set; }

    public DateTimeOffset PlannedEndsAt { get; private set; }

    public MentorshipStatus Status { get; private set; } = MentorshipStatus.Active;

    public DateTimeOffset? EndedAt { get; private set; }

    public MentorshipEndReasonCode? EndReasonCode { get; private set; }

    public string? EndReason { get; private set; }

    public bool TryComplete(DateTimeOffset now, string? reason) => Close(MentorshipStatus.Completed, now, null, reason);

    public bool TryTerminate(DateTimeOffset now, MentorshipEndReasonCode reasonCode, string? reason) =>
        Close(MentorshipStatus.Terminated, now, reasonCode, reason);

    internal static int RequireDurationMonths(int durationMonths) => durationMonths is 1 or 3 or 6
        ? durationMonths
        : throw new DomainException(nameof(DurationMonths), "Выберите длительность 1, 3 или 6 месяцев.");

    private bool Close(MentorshipStatus status, DateTimeOffset now, MentorshipEndReasonCode? reasonCode, string? reason)
    {
        if (Status == status)
        {
            return true;
        }

        if (Status != MentorshipStatus.Active)
        {
            return false;
        }

        if (reasonCode is { } code && !Enum.IsDefined(code))
        {
            throw new DomainException(nameof(reasonCode), "Выберите причину прекращения наставничества.");
        }

        EndReason = string.IsNullOrWhiteSpace(reason) ? null : Require.Text(reason, 1, EndReasonMaxLength, nameof(reason));
        EndReasonCode = reasonCode;
        EndedAt = now.ToUniversalTime();
        Status = status;
        return true;
    }
}
