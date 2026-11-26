using Application.Files;
using Application.Profiles;
using Domain.Catalogs;
using Domain.Mentors;

namespace Application.Mentors;

public sealed class MentorProfileService(
    IMentorProfileStore store,
    ProfileInputValidator validator,
    IFileStorage storage,
    TimeProvider timeProvider)
{
    public async Task<MentorProfileView?> FindAsync(Guid userId, CancellationToken cancellationToken)
    {
        var snapshot = await store.FindAsync(userId, cancellationToken);

        return snapshot is null ? null : ToView(snapshot);
    }

    public async Task<IReadOnlyList<MentorWorkplaceView>?> ReplaceWorkplacesAsync(
        Guid userId, IReadOnlyList<MentorWorkplaceInput> workplaces, CancellationToken cancellationToken)
    {
        var saved = await store.ReplaceWorkplacesAsync(userId, workplaces, timeProvider.GetUtcNow(), cancellationToken);
        return saved is null ? null : MentorWorkplaceView.From(saved);
    }

    public async Task<SaveMentorProfileResult> SaveAsync(SaveMentorProfileCommand command, CancellationToken cancellationToken)
    {
        var answers = new MentorAnswers(
            command.PositionId,
            command.ExperienceId,
            command.Skills,
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
            new(nameof(command.Skills), CatalogKinds.Skills, command.Skills.Select(skill => skill.SkillId).ToList()),
            new(nameof(command.Skills), CatalogKinds.SkillLevels, command.Skills.Select(skill => skill.SkillLevelId).ToList()),
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

    private MentorProfileView ToView(MentorProfileSnapshot snapshot) =>
        MentorProfileView.From(
            snapshot.Personal,
            snapshot.Mentor,
            snapshot.PhotoStorageKey is null ? null : storage.GetPublicUrl(snapshot.PhotoStorageKey));
}
