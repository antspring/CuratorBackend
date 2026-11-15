using Domain.Common;

namespace Domain.Catalogs;

public sealed class CatalogItem
{
    private CatalogItem()
    {
    }

    public CatalogItem(string kind, string code, string name, short sortOrder)
    {
        if (!CatalogKinds.All.Contains(kind))
        {
            throw new DomainException(nameof(Kind), $"Неизвестный справочник '{kind}'.");
        }

        Id = Guid.CreateVersion7();
        Kind = kind;
        Code = CatalogRules.RequireCode(code);
        Update(name, sortOrder);
    }

    public Guid Id { get; private set; }

    public string Kind { get; private set; } = null!;

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public short SortOrder { get; private set; }

    public void Update(string name, short sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new DomainException(nameof(SortOrder), "Порядок не может быть отрицательным.");
        }

        Name = CatalogRules.RequireName(name);
        SortOrder = sortOrder;
    }
}
