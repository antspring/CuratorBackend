using Domain.Catalogs;
using Domain.Common;

namespace UnitTests.Domain;

public class CatalogTests
{
    [Fact]
    public void Skill_TrimsNameAndNormalizesIt()
    {
        var skill = new Skill("csharp", "  C# ");

        Assert.NotEqual(Guid.Empty, skill.Id);
        Assert.Equal("C#", skill.Name);
        Assert.Equal("C#", skill.NormalizedName);
    }

    [Fact]
    public void Rename_UpdatesNormalizedName()
    {
        var city = new City("yekaterinburg", "Екатеринбург");

        city.Rename("екб");

        Assert.Equal("екб", city.Name);
        Assert.Equal("ЕКБ", city.NormalizedName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("CSharp")]
    [InlineData("c sharp")]
    [InlineData("-csharp")]
    [InlineData("c--sharp")]
    public void Constructor_RejectsInvalidCode(string code)
    {
        var exception = Assert.Throws<DomainException>(() => new Skill(code, "C#"));

        Assert.Equal("Code", exception.Field);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyName(string name)
    {
        var exception = Assert.Throws<DomainException>(() => new City("moscow", name));

        Assert.Equal("Name", exception.Field);
    }

    [Fact]
    public void Constructor_RejectsTooLongName()
    {
        var name = new string('а', CatalogRules.NameMaxLength + 1);

        var exception = Assert.Throws<DomainException>(() => new Skill("long", name));

        Assert.Equal("Name", exception.Field);
    }

    [Fact]
    public void CatalogItem_RejectsUnknownKind()
    {
        var exception = Assert.Throws<DomainException>(() => new CatalogItem("interests", "music", "Музыка", 0));

        Assert.Equal(nameof(CatalogItem.Kind), exception.Field);
    }

    [Fact]
    public void CatalogItem_RejectsNegativeSortOrder()
    {
        var exception = Assert.Throws<DomainException>(
            () => new CatalogItem(CatalogKinds.SkillLevels, "basic", "Базовый", -1));

        Assert.Equal(nameof(CatalogItem.SortOrder), exception.Field);
    }

    [Fact]
    public void CatalogItem_Update_ChangesNameAndSortOrder()
    {
        var item = new CatalogItem(CatalogKinds.SkillLevels, "basic", "Базовый", 0);

        item.Update(" Начальный ", 3);

        Assert.Equal("Начальный", item.Name);
        Assert.Equal(3, item.SortOrder);
    }
}
