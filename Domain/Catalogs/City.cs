namespace Domain.Catalogs;

public sealed class City : SearchableCatalogEntry
{
    private City()
    {
    }

    public City(string code, string name)
        : base(code, name)
    {
    }
}
