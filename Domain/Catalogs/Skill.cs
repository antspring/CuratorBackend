namespace Domain.Catalogs;

public sealed class Skill : SearchableCatalogEntry
{
    private Skill()
    {
    }

    public Skill(string code, string name)
        : base(code, name)
    {
    }
}
