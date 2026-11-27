using Domain.Catalogs;
using Domain.Mentors;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class MentorProfileConfiguration : IEntityTypeConfiguration<MentorProfile>
{
    public void Configure(EntityTypeBuilder<MentorProfile> builder)
    {
        builder.ToTable("mentor_profiles");
        builder.HasKey(profile => profile.UserId);

        builder.Property(profile => profile.UserId).ValueGeneratedNever();
        builder.Property(profile => profile.Price).HasPrecision(12, 2);

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<MentorProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<CatalogItem>().WithMany().HasForeignKey(profile => profile.PositionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CatalogItem>().WithMany().HasForeignKey(profile => profile.ExperienceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CatalogItem>().WithMany().HasForeignKey(profile => profile.FormatId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CatalogItem>().WithMany().HasForeignKey(profile => profile.AvailabilityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CatalogItem>().WithMany().HasForeignKey(profile => profile.PricingModelId).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(profile => profile.Availability,
            availability => ProfileAvailabilityConfiguration.Configure(availability, "mentor_availability", "MentorId"));

        builder.HasMany(profile => profile.Workplaces)
            .WithOne()
            .HasForeignKey(workplace => workplace.MentorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(profile => profile.OwnedSkills)
            .WithOne()
            .HasForeignKey(skill => skill.MentorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(profile => profile.Skills)
            .WithOne()
            .HasForeignKey(skill => skill.MentorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(profile => profile.Directions)
            .WithOne()
            .HasForeignKey(direction => direction.MentorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class MentorSkillConfiguration : IEntityTypeConfiguration<MentorSkill>
{
    public void Configure(EntityTypeBuilder<MentorSkill> builder)
    {
        builder.ToTable("mentor_skills");
        builder.HasKey(skill => new { skill.MentorId, skill.SkillId });

        builder.HasOne<Skill>().WithMany().HasForeignKey(skill => skill.SkillId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CatalogItem>().WithMany().HasForeignKey(skill => skill.SkillLevelId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MentorExpectedSkillConfiguration : IEntityTypeConfiguration<MentorExpectedSkill>
{
    public void Configure(EntityTypeBuilder<MentorExpectedSkill> builder)
    {
        builder.ToTable("mentor_expected_mentee_skills");
        builder.HasKey(skill => new { skill.MentorId, skill.SkillId });

        builder.HasOne<Skill>().WithMany().HasForeignKey(skill => skill.SkillId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CatalogItem>().WithMany().HasForeignKey(skill => skill.SkillLevelId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MentorDirectionConfiguration : IEntityTypeConfiguration<MentorDirection>
{
    public void Configure(EntityTypeBuilder<MentorDirection> builder)
    {
        builder.ToTable("mentor_directions");
        builder.HasKey(direction => new { direction.MentorId, direction.DirectionId });

        builder.HasOne<CatalogItem>().WithMany().HasForeignKey(direction => direction.DirectionId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MentorWorkplaceConfiguration : IEntityTypeConfiguration<MentorWorkplace>
{
    public void Configure(EntityTypeBuilder<MentorWorkplace> builder)
    {
        builder.ToTable("mentor_workplaces");
        builder.HasKey(workplace => new { workplace.MentorId, workplace.Id });
        builder.Property(workplace => workplace.Company).HasMaxLength(MentorWorkplace.TextMaxLength);
        builder.Property(workplace => workplace.Title).HasMaxLength(MentorWorkplace.TextMaxLength);
    }
}
