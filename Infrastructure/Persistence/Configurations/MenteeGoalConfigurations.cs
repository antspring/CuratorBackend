using Domain.Catalogs;
using Domain.Mentees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class MenteeGoalConfiguration : IEntityTypeConfiguration<MenteeGoal>
{
    public void Configure(EntityTypeBuilder<MenteeGoal> builder)
    {
        builder.ToTable("mentee_profile_goals");
        builder.HasKey(goal => new { goal.MenteeId, goal.Id });
        builder.Property(goal => goal.Text).HasMaxLength(MenteeAnswers.GoalMaxLength).IsRequired();

        builder.HasOne<CatalogItem>().WithMany().HasForeignKey(goal => goal.DeadlineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(goal => goal.Directions).WithOne()
            .HasForeignKey(direction => new { direction.MenteeId, direction.GoalId }).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class MenteeGoalDirectionConfiguration : IEntityTypeConfiguration<MenteeGoalDirection>
{
    public void Configure(EntityTypeBuilder<MenteeGoalDirection> builder)
    {
        builder.ToTable("mentee_profile_goal_directions");
        builder.HasKey(direction => new { direction.MenteeId, direction.GoalId, direction.DirectionId });

        builder.HasOne<CatalogItem>().WithMany().HasForeignKey(direction => direction.DirectionId).OnDelete(DeleteBehavior.Restrict);
    }
}
