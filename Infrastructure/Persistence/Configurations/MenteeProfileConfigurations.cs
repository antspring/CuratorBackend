using Domain.Catalogs;
using Domain.Mentees;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class MenteeProfileConfiguration : IEntityTypeConfiguration<MenteeProfile>
{
    public void Configure(EntityTypeBuilder<MenteeProfile> builder)
    {
        builder.ToTable("mentee_profiles");
        builder.HasKey(profile => profile.UserId);

        builder.Property(profile => profile.UserId).ValueGeneratedNever();
        builder.Property(profile => profile.Status).HasMaxLength(MenteeProfile.StatusMaxLength);
        builder.Property(profile => profile.University).HasMaxLength(MenteeProfile.EducationMaxLength);
        builder.Property(profile => profile.Specialty).HasMaxLength(MenteeProfile.EducationMaxLength);
        builder.Property(profile => profile.AboutMe).HasMaxLength(MenteeProfile.AboutMeMaxLength);
        builder.Property(profile => profile.BudgetFrom).HasPrecision(12, 2);
        builder.Property(profile => profile.BudgetTo).HasPrecision(12, 2);

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<MenteeProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<CatalogItem>().WithMany().HasForeignKey(profile => profile.FormatId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CatalogItem>().WithMany().HasForeignKey(profile => profile.AvailabilityId).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(profile => profile.Availability, availability =>
        {
            availability.ToTable("mentee_availability");
            availability.WithOwner().HasForeignKey("MenteeId");
            availability.Property(schedule => schedule.TimeZoneId).HasMaxLength(MenteeAvailability.TimeZoneIdMaxLength);
            availability.HasOne<City>().WithMany().HasForeignKey(schedule => schedule.MeetingCityId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.HasMany(profile => profile.Skills)
            .WithOne()
            .HasForeignKey(skill => skill.MenteeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(profile => profile.OwnedSkills)
            .WithOne()
            .HasForeignKey(skill => skill.MenteeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(profile => profile.Goals)
            .WithOne()
            .HasForeignKey(goal => goal.MenteeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(profile => profile.Tasks)
            .WithOne()
            .HasForeignKey(task => task.MenteeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class MenteeSkillConfiguration : IEntityTypeConfiguration<MenteeSkill>
{
    public void Configure(EntityTypeBuilder<MenteeSkill> builder)
    {
        builder.ToTable("mentee_skills");
        builder.HasKey(skill => new { skill.MenteeId, skill.SkillId });

        builder.HasOne<Skill>().WithMany().HasForeignKey(skill => skill.SkillId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MenteeTaskConfiguration : IEntityTypeConfiguration<MenteeTask>
{
    public void Configure(EntityTypeBuilder<MenteeTask> builder)
    {
        builder.ToTable("mentee_tasks");
        builder.HasKey(task => new { task.MenteeId, task.TaskId });

        builder.HasOne<CatalogItem>().WithMany().HasForeignKey(task => task.TaskId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MenteeOwnedSkillConfiguration : IEntityTypeConfiguration<MenteeOwnedSkill>
{
    public void Configure(EntityTypeBuilder<MenteeOwnedSkill> builder)
    {
        builder.ToTable("mentee_owned_skills");
        builder.HasKey(skill => new { skill.MenteeId, skill.SkillId });

        builder.HasOne<Skill>().WithMany().HasForeignKey(skill => skill.SkillId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CatalogItem>().WithMany().HasForeignKey(skill => skill.SkillLevelId).OnDelete(DeleteBehavior.Restrict);
    }
}
