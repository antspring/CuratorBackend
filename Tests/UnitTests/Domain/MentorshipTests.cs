using Domain.Common;
using Domain.Mentorships;

namespace UnitTests.Domain;

public class MentorshipTests
{
    private static readonly DateTimeOffset AcceptedAt = new(2026, 1, 31, 17, 30, 0, TimeSpan.FromHours(5));

    [Theory]
    [InlineData(1, 2, 28)]
    [InlineData(3, 4, 30)]
    [InlineData(6, 7, 31)]
    public void Constructor_UsesCalendarMonthsAndUtc(int durationMonths, int endMonth, int endDay)
    {
        var mentorship = Create(durationMonths);

        Assert.Equal(MentorshipStatus.Active, mentorship.Status);
        Assert.Equal(TimeSpan.Zero, mentorship.StartsAt.Offset);
        Assert.Equal(AcceptedAt, mentorship.StartsAt);
        Assert.Equal(new DateTimeOffset(2026, endMonth, endDay, 12, 30, 0, TimeSpan.Zero), mentorship.PlannedEndsAt);
        Assert.Null(mentorship.EndedAt);
    }

    [Fact]
    public void Constructor_RejectsUnsupportedDuration()
    {
        var exception = Assert.Throws<DomainException>(() => Create(2));

        Assert.Equal(nameof(Mentorship.DurationMonths), exception.Field);
    }

    [Fact]
    public void Constructor_RejectsSameParticipant()
    {
        var userId = Guid.NewGuid();

        var exception = Assert.Throws<DomainException>(() => new Mentorship(Guid.NewGuid(), userId, userId, 1, AcceptedAt));

        Assert.Equal(nameof(Mentorship.MentorId), exception.Field);
    }

    [Fact]
    public void TryComplete_KeepsFirstResultAndRejectsLaterTermination()
    {
        var mentorship = Create(3);
        var endedAt = AcceptedAt.AddDays(10);

        Assert.True(mentorship.TryComplete(endedAt, "Цели выполнены"));
        Assert.True(mentorship.TryComplete(endedAt.AddDays(1), "Другая причина"));
        Assert.False(mentorship.TryTerminate(endedAt.AddDays(2), MentorshipEndReasonCode.Other, null));

        Assert.Equal(MentorshipStatus.Completed, mentorship.Status);
        Assert.Equal(endedAt.ToUniversalTime(), mentorship.EndedAt);
        Assert.Equal("Цели выполнены", mentorship.EndReason);
        Assert.Null(mentorship.EndReasonCode);
    }

    [Fact]
    public void TryTerminate_KeepsOriginalTimeAndReasonCode()
    {
        var mentorship = Create(3);
        var endedAt = AcceptedAt.AddDays(5);

        Assert.True(mentorship.TryTerminate(endedAt, MentorshipEndReasonCode.GoalsChanged, null));
        Assert.True(mentorship.TryTerminate(endedAt.AddDays(1), MentorshipEndReasonCode.Other, "Новая причина"));
        Assert.False(mentorship.TryComplete(endedAt.AddDays(2), null));

        Assert.Equal(MentorshipStatus.Terminated, mentorship.Status);
        Assert.Equal(endedAt.ToUniversalTime(), mentorship.EndedAt);
        Assert.Equal(MentorshipEndReasonCode.GoalsChanged, mentorship.EndReasonCode);
        Assert.Null(mentorship.EndReason);
    }

    private static Mentorship Create(int durationMonths) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), durationMonths, AcceptedAt);
}
