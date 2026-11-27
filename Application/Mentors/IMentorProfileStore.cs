using Application.Profiles;
using Domain.Mentors;
using Domain.Users;

namespace Application.Mentors;

public interface IMentorProfileStore
{
    Task<MentorProfileSnapshot?> FindAsync(Guid userId, CancellationToken cancellationToken);

    Task<MentorProfileSnapshot?> PatchAsync(
        Guid userId, MentorProfilePatch patch, DateTimeOffset now, CancellationToken cancellationToken);

    Task<SaveProfileOutcome> SaveAsync(
        Guid userId,
        PersonalData personal,
        MentorAnswers answers,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record MentorProfileSnapshot(UserProfile Personal, MentorProfile Mentor, string? PhotoStorageKey);
