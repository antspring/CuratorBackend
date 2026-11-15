using Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder) =>
        SearchableCatalogEntryMapping.Configure(builder, "skills");
}

internal sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder) =>
        SearchableCatalogEntryMapping.Configure(builder, "cities");
}

internal sealed class CatalogItemConfiguration : IEntityTypeConfiguration<CatalogItem>
{
    public void Configure(EntityTypeBuilder<CatalogItem> builder)
    {
        builder.ToTable("catalog_items");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Kind).HasMaxLength(CatalogKinds.MaxLength);
        builder.Property(item => item.Code).HasMaxLength(CatalogRules.CodeMaxLength);
        builder.Property(item => item.Name).HasMaxLength(CatalogRules.NameMaxLength);

        builder.HasIndex(item => new { item.Kind, item.Code }).IsUnique();
    }
}

internal static class SearchableCatalogEntryMapping
{
    public static void Configure<TEntry>(EntityTypeBuilder<TEntry> builder, string table)
        where TEntry : SearchableCatalogEntry
    {
        builder.ToTable(table);
        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id).ValueGeneratedNever();
        builder.Property(entry => entry.Code).HasMaxLength(CatalogRules.CodeMaxLength);
        builder.Property(entry => entry.Name).HasMaxLength(CatalogRules.NameMaxLength);
        builder.Property(entry => entry.NormalizedName).HasMaxLength(CatalogRules.NameMaxLength);

        builder.HasIndex(entry => entry.Code).IsUnique();
        builder.HasIndex(entry => entry.NormalizedName).IsUnique();
    }
}
