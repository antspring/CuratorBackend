using Application.Catalogs;
using Application.Files;
using Domain.Catalogs;
using Domain.Common;
using Domain.Files;
using Domain.Users;

namespace Application.Profiles;

public sealed record CatalogReference(string Field, string Kind, IReadOnlyCollection<Guid> Ids);

public sealed class ProfileInputValidator(ICatalogLookup catalogs, IFileMetadataStore files)
{
    private const string PersonalField = "Personal";

    public static IEnumerable<CatalogReference> PersonalReferences(PersonalInput personal) =>
    [
        new($"{PersonalField}.{nameof(PersonalInput.CityId)}", CatalogKinds.Cities, [personal.CityId]),
        new($"{PersonalField}.{nameof(PersonalInput.LanguageId)}", CatalogKinds.Languages, [personal.LanguageId]),
    ];

    public async Task<PersonalData> BuildPersonalAsync(Guid userId, PersonalInput input, CancellationToken cancellationToken)
    {
        var photo = await FindPhotoAsync(userId, input.PhotoFileId, $"{PersonalField}.{nameof(input.PhotoFileId)}", cancellationToken);

        try
        {
            return new PersonalData(input.FirstName, input.LastName, input.Age, input.CityId, input.LanguageId, photo);
        }
        catch (DomainException exception)
        {
            throw new DomainException($"{PersonalField}.{exception.Field}", exception.Message);
        }
    }

    public async Task<StoredFile?> FindPhotoAsync(Guid userId, Guid? photoFileId, string field, CancellationToken cancellationToken)
    {
        if (photoFileId is null)
        {
            return null;
        }

        return await files.FindOwnedAsync(photoFileId.Value, userId, cancellationToken)
            ?? throw new DomainException(field, "Файл не найден.");
    }

    public async Task EnsureCatalogReferencesAsync(IReadOnlyCollection<CatalogReference> references, CancellationToken cancellationToken)
    {
        var ids = references.SelectMany(reference => reference.Ids).Distinct().ToList();
        var entries = await catalogs.FindAsync(ids, cancellationToken);

        foreach (var reference in references)
        {
            if (reference.Ids.Any(id => entries.GetValueOrDefault(id)?.Kind != reference.Kind))
            {
                throw new DomainException(reference.Field, "Значение не найдено в справочнике.");
            }
        }
    }
}
