using Application.Profiles;
using Domain.Mentors;
using Domain.Users;

namespace Application.Mentors;

public sealed record SaveMentorProfileCommand(
    Guid UserId,
    PersonalInput Personal,
    Guid PositionId,
    Guid ExperienceId,
    IReadOnlyList<MentorSkillChoice> Skills,
    IReadOnlyList<Guid> DirectionIds,
    Guid FormatId,
    Guid AvailabilityId,
    Guid PricingModelId,
    decimal Price,
    bool HasFreeTrial);

public sealed record MentorProfileView(
    Guid UserId,
    PersonalView Personal,
    Guid PositionId,
    Guid ExperienceId,
    IReadOnlyList<MentorSkillChoice> Skills,
    IReadOnlyList<Guid> DirectionIds,
    Guid FormatId,
    Guid AvailabilityId,
    Guid PricingModelId,
    decimal Price,
    bool HasFreeTrial,
    DateTimeOffset UpdatedAt)
{
    public static MentorProfileView From(UserProfile personal, MentorProfile mentor, Uri? photoUrl) =>
        new(
            mentor.UserId,
            PersonalView.From(personal, photoUrl),
            mentor.PositionId,
            mentor.ExperienceId,
            mentor.Skills.Select(skill => new MentorSkillChoice(skill.SkillId, skill.SkillLevelId)).OrderBy(skill => skill.SkillId).ToList(),
            mentor.Directions.Select(direction => direction.DirectionId).Order().ToList(),
            mentor.FormatId,
            mentor.AvailabilityId,
            mentor.PricingModelId,
            mentor.Price,
            mentor.HasFreeTrial,
            personal.UpdatedAt);
}

public abstract record SaveMentorProfileResult
{
    public sealed record Saved(MentorProfileView Profile) : SaveMentorProfileResult;

    public sealed record AccountNotFound : SaveMentorProfileResult;

    public sealed record RoleConflict : SaveMentorProfileResult;

    public sealed record Forbidden : SaveMentorProfileResult;
}
