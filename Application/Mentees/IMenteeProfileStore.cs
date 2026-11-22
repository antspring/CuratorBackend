using Application.Profiles;
using Domain.Mentees;
using Domain.Users;

namespace Application.Mentees;

public interface IMenteeProfileStore
{
    Task<MenteeProfileSnapshot?> FindAsync(Guid userId, CancellationToken cancellationToken);

    Task<SaveProfileOutcome> SaveAsync(
        Guid userId,
        PersonalData personal,
        MenteeAnswers answers,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record MenteeProfileSnapshot(UserProfile Personal, MenteeProfile Mentee, string? PhotoStorageKey);
