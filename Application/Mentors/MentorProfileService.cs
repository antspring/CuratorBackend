using Application.Catalogs;
using Application.Files;
using Application.Profiles;
using Domain.Catalogs;
using Domain.Mentors;

namespace Application.Mentors;

public sealed class MentorProfileService(
    IMentorProfileStore store,
    ProfileInputValidator validator,
    ICatalogLookup catalogs,
    IFileStorage storage,
    TimeProvider timeProvider)
{
    public async Task<MentorProfileView?> FindAsync(Guid userId, CancellationToken cancellationToken)
    {
        var snapshot = await store.FindAsync(userId, cancellationToken);

        return snapshot is null ? null : await ToViewAsync(snapshot, cancellationToken);
    }

    public async Task<MentorProfileView?> PatchAsync(
        Guid userId, MentorProfilePatch patch, CancellationToken cancellationToken)
    {
        var references = ProfileInputValidator.AvailabilityReferences(patch.Availability).ToList();
        if (patch.Skills is { Count: > 0 } skills)
        {
            references.AddRange(SkillReferences(nameof(patch.Skills), skills));
        }

        await validator.EnsureCatalogReferencesAsync(references, cancellationToken);
        var snapshot = await store.PatchAsync(userId, patch, timeProvider.GetUtcNow(), cancellationToken);
        return snapshot is null ? null : await ToViewAsync(snapshot, cancellationToken);
    }

    public async Task<SaveMentorProfileResult> SaveAsync(SaveMentorProfileCommand command, CancellationToken cancellationToken)
    {
        var answers = new MentorAnswers(
            command.PositionId,
            command.ExperienceId,
            command.OwnedSkills,
            command.DirectionIds,
            command.FormatId,
            command.AvailabilityId,
            command.PricingModelId,
            command.Price,
            command.HasFreeTrial);

        var personal = await validator.BuildPersonalAsync(command.UserId, command.Personal, cancellationToken);

        CatalogReference[] references =
        [
            .. ProfileInputValidator.PersonalReferences(command.Personal),
            new(nameof(command.PositionId), CatalogKinds.Positions, [command.PositionId]),
            new(nameof(command.ExperienceId), CatalogKinds.Experience, [command.ExperienceId]),
            .. SkillReferences(nameof(command.OwnedSkills), answers.OwnedSkills),
            new(nameof(command.DirectionIds), CatalogKinds.MentoringDirections, command.DirectionIds),
            new(nameof(command.FormatId), CatalogKinds.Formats, [command.FormatId]),
            new(nameof(command.AvailabilityId), CatalogKinds.Availability, [command.AvailabilityId]),
            new(nameof(command.PricingModelId), CatalogKinds.PricingModels, [command.PricingModelId]),
        ];

        await validator.EnsureCatalogReferencesAsync(references, cancellationToken);

        var outcome = await store.SaveAsync(command.UserId, personal, answers, timeProvider.GetUtcNow(), cancellationToken);

        return outcome switch
        {
            SaveProfileOutcome.Saved => new SaveMentorProfileResult.Saved(
                (await FindAsync(command.UserId, cancellationToken))!),
            SaveProfileOutcome.AccountNotFound => new SaveMentorProfileResult.AccountNotFound(),
            SaveProfileOutcome.RoleConflict => new SaveMentorProfileResult.RoleConflict(),
            SaveProfileOutcome.Forbidden => new SaveMentorProfileResult.Forbidden(),
            _ => throw new InvalidOperationException($"Неизвестный результат сохранения анкеты: {outcome}"),
        };
    }

    private static CatalogReference[] SkillReferences(string field, IReadOnlyList<MentorSkillChoice> skills) =>
    [
        new(field, CatalogKinds.Skills, skills.Select(skill => skill.SkillId).ToArray()),
        new(field, CatalogKinds.SkillLevels, skills.Select(skill => skill.SkillLevelId).ToArray()),
    ];

    private async Task<MentorProfileView> ToViewAsync(MentorProfileSnapshot snapshot, CancellationToken cancellationToken)
    {
        var meetingCity = snapshot.Mentor.Availability?.MeetingCityId is { } cityId
            ? (await catalogs.FindAsync([cityId], cancellationToken))[cityId]
            : null;

        return MentorProfileView.From(
            snapshot.Personal,
            snapshot.Mentor,
            snapshot.PhotoStorageKey is null ? null : storage.GetPublicUrl(snapshot.PhotoStorageKey),
            meetingCity);
    }
}
