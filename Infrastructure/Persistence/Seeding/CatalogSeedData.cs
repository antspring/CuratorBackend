using System.Text.Json;
using Domain.Catalogs;

namespace Infrastructure.Persistence.Seeding;

internal sealed record CatalogSeedEntry(string Code, string Name);

internal sealed record CatalogSeedData(
    IReadOnlyList<CatalogSeedEntry> Skills,
    IReadOnlyList<CatalogSeedEntry> Cities,
    IReadOnlyDictionary<string, IReadOnlyList<CatalogSeedEntry>> Items)
{
    public const string ResourceName = "catalogs.json";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
    };

    public static CatalogSeedData Load()
    {
        using var stream = typeof(CatalogSeedData).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Не найден встроенный ресурс '{ResourceName}'.");

        var data = JsonSerializer.Deserialize<CatalogSeedData>(stream, SerializerOptions)
            ?? throw new InvalidOperationException($"Ресурс '{ResourceName}' пуст.");

        data.Validate();
        return data;
    }

    public void Validate()
    {
        RequireUnique("skills", Skills, normalizeNames: true);
        RequireUnique("cities", Cities, normalizeNames: true);

        foreach (var (kind, entries) in Items)
        {
            if (!CatalogKinds.ItemKinds.Contains(kind))
            {
                throw new InvalidOperationException($"Неизвестный справочник '{kind}' в сид-данных.");
            }

            if (entries.Count > short.MaxValue)
            {
                throw new InvalidOperationException($"Справочник '{kind}' слишком большой для sort_order.");
            }

            RequireUnique(kind, entries, normalizeNames: false);
        }
    }

    private static void RequireUnique(string catalog, IReadOnlyList<CatalogSeedEntry> entries, bool normalizeNames)
    {
        var duplicateCode = entries.GroupBy(entry => entry.Code).FirstOrDefault(group => group.Count() > 1);

        if (duplicateCode is not null)
        {
            throw new InvalidOperationException($"В справочнике '{catalog}' повторяется code '{duplicateCode.Key}'.");
        }

        if (!normalizeNames)
        {
            return;
        }

        var duplicateName = entries
            .GroupBy(entry => CatalogRules.Normalize(entry.Name))
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateName is not null)
        {
            throw new InvalidOperationException($"В справочнике '{catalog}' повторяется название '{duplicateName.Key}'.");
        }
    }
}
