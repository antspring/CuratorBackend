using Domain.Files;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("files");
        builder.HasKey(file => file.Id);

        builder.Property(file => file.Id).ValueGeneratedNever();
        builder.Property(file => file.Purpose).HasMaxLength(FilePurposes.MaxLength);
        builder.Property(file => file.Name).HasColumnName("original_name").HasMaxLength(StoredFile.NameMaxLength);
        builder.Property(file => file.ContentType).HasMaxLength(StoredFile.ContentTypeMaxLength);
        builder.Property(file => file.StorageKey).HasMaxLength(StoredFile.StorageKeyMaxLength);

        builder.HasIndex(file => file.StorageKey).IsUnique();
        builder.HasIndex(file => file.OwnerId);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(file => file.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
