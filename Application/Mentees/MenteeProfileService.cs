using Application.Files;
using Application.Profiles;
using Domain.Catalogs;
using Domain.Mentees;

namespace Application.Mentees;

public sealed class MenteeProfileService(
    IMenteeProfileStore store,
    ProfileInputValidator validator,
    IFileStorage storage,
    TimeProvider timeProvider)
{
    public async Task<MenteeProfileView?> FindAsync(Guid userId, CancellationToken cancellationToken)
    {
        var snapshot = await store.FindAsync(userId, cancellationToken);

        return snapshot is null ? null : ToView(snapshot);
    }

    public async Task<MenteeProfileView?> UpdatePersonalAsync(SaveMenteePersonalCommand command, CancellationToken cancellationToken)
    {
        await validator.EnsureCatalogReferencesAsync(
            [new(nameof(command.CityId), CatalogKinds.Cities, [command.CityId])], cancellationToken);
        var photo = await validator.FindPhotoAsync(command.UserId, command.PhotoFileId, nameof(command.PhotoFileId), cancellationToken);
        var updated = await store.UpdatePersonalAsync(command, photo, timeProvider.GetUtcNow(), cancellationToken);
        return updated ? await FindAsync(command.UserId, cancellationToken) : null;
    }

    public async Task<bool> ReplaceOwnedSkillsAsync(
        Guid userId, IReadOnlyList<MenteeOwnedSkillChoice> choices, CancellationToken cancellationToken)
    {
        await validator.EnsureCatalogReferencesAsync(
        [
            new(nameof(MenteeProfile.OwnedSkills), CatalogKinds.Skills, choices.Select(choice => choice.SkillId).ToArray()),
            new(nameof(MenteeProfile.OwnedSkills), CatalogKinds.SkillLevels, choices.Select(choice => choice.SkillLevelId).ToArray()),
        ], cancellationToken);

        return await store.ReplaceOwnedSkillsAsync(userId, choices, timeProvider.GetUtcNow(), cancellationToken);
    }

    public async Task<bool> ReplaceGoalsAsync(
        Guid userId, IReadOnlyList<MenteeGoalChoice> choices, CancellationToken cancellationToken)
    {
        var references = choices.SelectMany((choice, index) => new CatalogReference[]
        {
            new($"Goals[{index}].{nameof(choice.DeadlineId)}", CatalogKinds.GoalDeadlines, [choice.DeadlineId]),
            new($"Goals[{index}].{nameof(choice.DirectionIds)}", CatalogKinds.MentoringDirections, choice.DirectionIds),
        }).ToArray();
        await validator.EnsureCatalogReferencesAsync(references, cancellationToken);

        return await store.ReplaceGoalsAsync(userId, choices, timeProvider.GetUtcNow(), cancellationToken);
    }

    public async Task<SaveMenteeProfileResult> SaveAsync(SaveMenteeProfileCommand command, CancellationToken cancellationToken)
    {
        var answers = new MenteeAnswers(
            command.SkillIds,
            command.SkillLevelId,
            command.TaskIds,
            command.FormatId,
            command.AvailabilityId,
            command.BudgetFrom,
            command.BudgetTo,
            command.Goal,
            command.GoalDeadlineId);

        var personal = await validator.BuildPersonalAsync(command.UserId, command.Personal, cancellationToken);

        CatalogReference[] references =
        [
            .. ProfileInputValidator.PersonalReferences(command.Personal),
            new(nameof(command.SkillIds), CatalogKinds.Skills, command.SkillIds),
            new(nameof(command.SkillLevelId), CatalogKinds.SkillLevels, [command.SkillLevelId]),
            new(nameof(command.TaskIds), CatalogKinds.MenteeTasks, command.TaskIds),
            new(nameof(command.FormatId), CatalogKinds.Formats, [command.FormatId]),
            new(nameof(command.AvailabilityId), CatalogKinds.Availability, [command.AvailabilityId]),
            new(nameof(command.GoalDeadlineId), CatalogKinds.GoalDeadlines, [command.GoalDeadlineId]),
        ];

        await validator.EnsureCatalogReferencesAsync(references, cancellationToken);

        var outcome = await store.SaveAsync(command.UserId, personal, answers, timeProvider.GetUtcNow(), cancellationToken);

        return outcome switch
        {
            SaveProfileOutcome.Saved => new SaveMenteeProfileResult.Saved(
                (await FindAsync(command.UserId, cancellationToken))!),
            SaveProfileOutcome.AccountNotFound => new SaveMenteeProfileResult.AccountNotFound(),
            SaveProfileOutcome.RoleConflict => new SaveMenteeProfileResult.RoleConflict(),
            SaveProfileOutcome.Forbidden => new SaveMenteeProfileResult.Forbidden(),
            _ => throw new InvalidOperationException($"Неизвестный результат сохранения анкеты: {outcome}"),
        };
    }

    private MenteeProfileView ToView(MenteeProfileSnapshot snapshot) =>
        MenteeProfileView.From(
            snapshot.Personal,
            snapshot.Mentee,
            snapshot.PhotoStorageKey is null ? null : storage.GetPublicUrl(snapshot.PhotoStorageKey));
}
