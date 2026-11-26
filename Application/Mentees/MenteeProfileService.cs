using Application.Catalogs;
using Application.Files;
using Application.Profiles;
using Domain.Catalogs;
using Domain.Mentees;

namespace Application.Mentees;

public sealed class MenteeProfileService(
    IMenteeProfileStore store,
    ProfileInputValidator validator,
    ICatalogLookup catalogs,
    IFileStorage storage,
    TimeProvider timeProvider)
{
    public async Task<MenteeProfileView?> FindAsync(Guid userId, CancellationToken cancellationToken)
    {
        var snapshot = await store.FindAsync(userId, cancellationToken);

        if (snapshot is null)
        {
            return null;
        }

        var mentee = snapshot.Mentee;
        Guid[] ids =
        [
            snapshot.Personal.CityId,
            snapshot.Personal.LanguageId,
            mentee.SkillLevelId,
            mentee.FormatId,
            mentee.AvailabilityId,
            mentee.GoalDeadlineId,
            .. mentee.Skills.Select(skill => skill.SkillId),
            .. mentee.Tasks.Select(task => task.TaskId),
            .. mentee.OwnedSkills.Select(skill => skill.SkillId),
            .. mentee.OwnedSkills.Select(skill => skill.SkillLevelId),
            .. mentee.Goals.Select(goal => goal.DeadlineId),
            .. mentee.Goals.SelectMany(goal => goal.Directions.Select(direction => direction.DirectionId)),
            .. (mentee.Availability?.MeetingCityId is { } cityId ? new[] { cityId } : Array.Empty<Guid>()),
        ];

        var entries = await catalogs.FindAsync(ids.Distinct().ToArray(), cancellationToken);
        var photoUrl = snapshot.PhotoStorageKey is { } photoKey ? storage.GetPublicUrl(photoKey) : null;
        return MenteeProfileView.From(snapshot.Personal, mentee, photoUrl, entries);
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

    public async Task<bool> UpdateAvailabilityAsync(
        Guid userId, MenteeAvailability availability, CancellationToken cancellationToken)
    {
        if (availability.MeetingCityId is { } cityId)
        {
            await validator.EnsureCatalogReferencesAsync(
                [new(nameof(availability.MeetingCityId), CatalogKinds.Cities, [cityId])], cancellationToken);
        }

        return await store.UpdateAvailabilityAsync(userId, availability, timeProvider.GetUtcNow(), cancellationToken);
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
}
