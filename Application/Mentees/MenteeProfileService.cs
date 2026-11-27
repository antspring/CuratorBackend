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
        return snapshot is null ? null : await ToViewAsync(snapshot, cancellationToken);
    }

    private async Task<MenteeProfileView> ToViewAsync(MenteeProfileSnapshot snapshot, CancellationToken cancellationToken)
    {
        var mentee = snapshot.Mentee;
        Guid[] ids =
        [
            snapshot.Personal.CityId,
            snapshot.Personal.LanguageId,
            mentee.FormatId,
            mentee.AvailabilityId,
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

    public async Task<MenteeProfileView?> PatchAsync(Guid userId, MenteeProfilePatch patch, CancellationToken cancellationToken)
    {
        var references = new List<CatalogReference>();
        if (patch.Personal is { } personal)
        {
            references.Add(new("Personal.CityId", CatalogKinds.Cities, [personal.CityId]));
        }

        if (patch.OwnedSkills is { } ownedSkills)
        {
            references.AddRange(OwnedSkillReferences(ownedSkills));
        }

        if (patch.Goals is { } goals)
        {
            references.AddRange(GoalReferences(goals));
        }

        if (patch.Availability?.MeetingCityId is { } cityId)
        {
            references.Add(new("Availability.MeetingCityId", CatalogKinds.Cities, [cityId]));
        }

        await validator.EnsureCatalogReferencesAsync(references, cancellationToken);
        var photo = await validator.FindPhotoAsync(userId, patch.Personal?.PhotoFileId, "Personal.PhotoFileId", cancellationToken);
        var snapshot = await store.PatchAsync(userId, patch, photo, timeProvider.GetUtcNow(), cancellationToken);
        return snapshot is null ? null : await ToViewAsync(snapshot, cancellationToken);
    }

    public async Task<SaveMenteeProfileResult> SaveAsync(SaveMenteeProfileCommand command, CancellationToken cancellationToken)
    {
        var answers = new MenteeAnswers(
            command.SkillIds,
            command.OwnedSkills,
            command.TaskIds,
            command.FormatId,
            command.AvailabilityId,
            command.BudgetFrom,
            command.BudgetTo,
            command.Goals);

        var personal = await validator.BuildPersonalAsync(command.UserId, command.Personal, cancellationToken);

        CatalogReference[] references =
        [
            .. ProfileInputValidator.PersonalReferences(command.Personal),
            new(nameof(command.SkillIds), CatalogKinds.Skills, command.SkillIds),
            .. OwnedSkillReferences(answers.OwnedSkills),
            new(nameof(command.TaskIds), CatalogKinds.MenteeTasks, command.TaskIds),
            new(nameof(command.FormatId), CatalogKinds.Formats, [command.FormatId]),
            new(nameof(command.AvailabilityId), CatalogKinds.Availability, [command.AvailabilityId]),
            .. GoalReferences(answers.Goals),
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

    private static CatalogReference[] OwnedSkillReferences(IReadOnlyList<MenteeOwnedSkillChoice> skills) =>
    [
        new(nameof(MenteeProfile.OwnedSkills), CatalogKinds.Skills, skills.Select(skill => skill.SkillId).ToArray()),
        new(nameof(MenteeProfile.OwnedSkills), CatalogKinds.SkillLevels, skills.Select(skill => skill.SkillLevelId).ToArray()),
    ];

    private static IEnumerable<CatalogReference> GoalReferences(IReadOnlyList<MenteeGoalChoice> goals) =>
        goals.SelectMany((goal, index) => new CatalogReference[]
        {
            new($"Goals[{index}].{nameof(goal.DeadlineId)}", CatalogKinds.GoalDeadlines, [goal.DeadlineId]),
            new($"Goals[{index}].{nameof(goal.DirectionIds)}", CatalogKinds.MentoringDirections, goal.DirectionIds),
        });
}
