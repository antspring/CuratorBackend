namespace Domain.Catalogs;

public abstract class SearchableCatalogEntry
{
    protected SearchableCatalogEntry()
    {
    }

    protected SearchableCatalogEntry(string code, string name)
    {
        Id = Guid.CreateVersion7();
        Code = CatalogRules.RequireCode(code);
        Rename(name);
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string NormalizedName { get; private set; } = null!;

    public void Rename(string name)
    {
        Name = CatalogRules.RequireName(name);
        NormalizedName = CatalogRules.Normalize(Name);
    }
}
