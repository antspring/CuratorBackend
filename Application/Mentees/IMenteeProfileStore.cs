using Application.Profiles;
using Domain.Files;
using Domain.Mentees;
using Domain.Users;

namespace Application.Mentees;

public interface IMenteeProfileStore
{
    Task<MenteeProfileSnapshot?> FindAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> UpdatePersonalAsync(
        SaveMenteePersonalCommand command, StoredFile? photo, DateTimeOffset now, CancellationToken cancellationToken);

    Task<bool> ReplaceOwnedSkillsAsync(
        Guid userId, IReadOnlyList<MenteeOwnedSkillChoice> choices, DateTimeOffset now, CancellationToken cancellationToken);

    Task<bool> ReplaceGoalsAsync(
        Guid userId, IReadOnlyList<MenteeGoalChoice> choices, DateTimeOffset now, CancellationToken cancellationToken);

    Task<SaveProfileOutcome> SaveAsync(
        Guid userId,
        PersonalData personal,
        MenteeAnswers answers,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record MenteeProfileSnapshot(UserProfile Personal, MenteeProfile Mentee, string? PhotoStorageKey);
