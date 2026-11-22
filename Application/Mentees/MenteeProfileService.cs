using Application.Catalogs;
using Application.Files;
using Application.Profiles;
using Domain.Catalogs;
using Domain.Common;
using Domain.Files;
using Domain.Mentees;
using Domain.Users;

namespace Application.Mentees;

public sealed class MenteeProfileService(
    IMenteeProfileStore store,
    ICatalogLookup catalogs,
    IFileMetadataStore files,
    IFileStorage storage,
    TimeProvider timeProvider)
{
    private const string PersonalField = nameof(SaveMenteeProfileCommand.Personal);

    public async Task<MenteeProfileView?> FindAsync(Guid userId, CancellationToken cancellationToken)
    {
        var snapshot = await store.FindAsync(userId, cancellationToken);

        return snapshot is null ? null : ToView(snapshot);
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

        var personal = await BuildPersonalAsync(command.UserId, command.Personal, cancellationToken);
        await EnsureCatalogReferencesAsync(command, cancellationToken);

        var outcome = await store.SaveAsync(command.UserId, personal, answers, timeProvider.GetUtcNow(), cancellationToken);

        return outcome switch
        {
            SaveMenteeProfileOutcome.Saved => new SaveMenteeProfileResult.Saved(
                (await FindAsync(command.UserId, cancellationToken))!),
            SaveMenteeProfileOutcome.AccountNotFound => new SaveMenteeProfileResult.AccountNotFound(),
            SaveMenteeProfileOutcome.RoleConflict => new SaveMenteeProfileResult.RoleConflict(),
            SaveMenteeProfileOutcome.Forbidden => new SaveMenteeProfileResult.Forbidden(),
            _ => throw new InvalidOperationException($"Неизвестный результат сохранения анкеты: {outcome}"),
        };
    }

    private async Task<PersonalData> BuildPersonalAsync(Guid userId, PersonalInput input, CancellationToken cancellationToken)
    {
        StoredFile? photo = null;

        if (input.PhotoFileId is { } photoFileId)
        {
            photo = await files.FindOwnedAsync(photoFileId, userId, cancellationToken)
                ?? throw new DomainException($"{PersonalField}.{nameof(PersonalInput.PhotoFileId)}", "Файл не найден.");
        }

        try
        {
            return new PersonalData(input.FirstName, input.LastName, input.Age, input.CityId, input.LanguageId, photo);
        }
        catch (DomainException exception)
        {
            throw new DomainException($"{PersonalField}.{exception.Field}", exception.Message);
        }
    }

    private async Task EnsureCatalogReferencesAsync(SaveMenteeProfileCommand command, CancellationToken cancellationToken)
    {
        (string Field, string Kind, IReadOnlyList<Guid> Ids)[] references =
        [
            ($"{PersonalField}.{nameof(PersonalInput.CityId)}", CatalogKinds.Cities, [command.Personal.CityId]),
            ($"{PersonalField}.{nameof(PersonalInput.LanguageId)}", CatalogKinds.Languages, [command.Personal.LanguageId]),
            (nameof(command.SkillIds), CatalogKinds.Skills, command.SkillIds),
            (nameof(command.SkillLevelId), CatalogKinds.SkillLevels, [command.SkillLevelId]),
            (nameof(command.TaskIds), CatalogKinds.MenteeTasks, command.TaskIds),
            (nameof(command.FormatId), CatalogKinds.Formats, [command.FormatId]),
            (nameof(command.AvailabilityId), CatalogKinds.Availability, [command.AvailabilityId]),
            (nameof(command.GoalDeadlineId), CatalogKinds.GoalDeadlines, [command.GoalDeadlineId]),
        ];

        var ids = references.SelectMany(reference => reference.Ids).Distinct().ToList();
        var kinds = await catalogs.FindKindsAsync(ids, cancellationToken);

        foreach (var (field, kind, referenceIds) in references)
        {
            if (referenceIds.Any(id => kinds.GetValueOrDefault(id) != kind))
            {
                throw new DomainException(field, "Значение не найдено в справочнике.");
            }
        }
    }

    private MenteeProfileView ToView(MenteeProfileSnapshot snapshot) =>
        MenteeProfileView.From(
            snapshot.Personal,
            snapshot.Mentee,
            snapshot.PhotoStorageKey is null ? null : storage.GetPublicUrl(snapshot.PhotoStorageKey));
}
