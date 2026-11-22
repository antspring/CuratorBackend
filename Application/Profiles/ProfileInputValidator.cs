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

    public async Task EnsureCatalogReferencesAsync(IReadOnlyCollection<CatalogReference> references, CancellationToken cancellationToken)
    {
        var ids = references.SelectMany(reference => reference.Ids).Distinct().ToList();
        var found = await catalogs.FindAsync(ids, cancellationToken);

        foreach (var reference in references)
        {
            if (reference.Ids.Any(id => found.GetValueOrDefault(id)?.Kind != reference.Kind))
            {
                throw new DomainException(reference.Field, "Значение не найдено в справочнике.");
            }
        }
    }
}
