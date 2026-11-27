using Domain.Mentees;
using Domain.Mentors;
using Domain.Mentorships;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class MentorshipRequestConfiguration : IEntityTypeConfiguration<MentorshipRequest>
{
    public void Configure(EntityTypeBuilder<MentorshipRequest> builder)
    {
        builder.ToTable("mentorship_requests");
        builder.HasKey(request => request.Id);
        builder.Property(request => request.Id).ValueGeneratedNever();
        builder.Property(request => request.DurationMonths).HasConversion<short>();
        builder.Property(request => request.Message).HasMaxLength(MentorshipRequest.MessageMaxLength);
        builder.Property(request => request.Status).HasMaxLength(16)
            .HasConversion(status => status.ToString().ToLowerInvariant(), value => Enum.Parse<MentorshipRequestStatus>(value, true));

        builder.HasIndex(request => new { request.MenteeId, request.MentorId })
            .IsUnique().HasFilter("status = 'pending'");
        builder.HasIndex(request => new { request.MenteeId, request.CreatedAt, request.Id });
        builder.HasIndex(request => new { request.MentorId, request.CreatedAt, request.Id });

        builder.HasOne<MenteeProfile>().WithMany().HasForeignKey(request => request.MenteeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<MentorProfile>().WithMany().HasForeignKey(request => request.MentorId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class MentorshipConfiguration : IEntityTypeConfiguration<Mentorship>
{
    public void Configure(EntityTypeBuilder<Mentorship> builder)
    {
        builder.ToTable("mentorships");
        builder.HasKey(mentorship => mentorship.Id);
        builder.Property(mentorship => mentorship.Id).ValueGeneratedNever();
        builder.Property(mentorship => mentorship.DurationMonths).HasConversion<short>();
        builder.Property(mentorship => mentorship.Status).HasMaxLength(16)
            .HasConversion(status => status.ToString().ToLowerInvariant(), value => Enum.Parse<MentorshipStatus>(value, true));
        builder.Property(mentorship => mentorship.EndReasonCode).HasConversion<string>().HasMaxLength(16);
        builder.Property(mentorship => mentorship.EndReason).HasMaxLength(Mentorship.EndReasonMaxLength);

        builder.HasIndex(mentorship => new { mentorship.MenteeId, mentorship.MentorId })
            .IsUnique().HasFilter("status = 'active'");
        builder.HasOne<MentorshipRequest>().WithOne().HasForeignKey<Mentorship>(mentorship => mentorship.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
