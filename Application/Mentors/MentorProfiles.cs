using Application.Catalogs;
using Application.Profiles;
using Domain.Mentors;
using Domain.Users;

namespace Application.Mentors;

public sealed record SaveMentorProfileCommand(
    Guid UserId,
    PersonalInput Personal,
    Guid PositionId,
    Guid ExperienceId,
    IReadOnlyList<MentorSkillChoice> OwnedSkills,
    IReadOnlyList<Guid> DirectionIds,
    Guid FormatId,
    Guid AvailabilityId,
    Guid PricingModelId,
    decimal Price,
    bool HasFreeTrial);

public sealed record MentorProfilePatch(
    IReadOnlyList<MentorWorkplaceInput>? Workplaces,
    IReadOnlyList<MentorSkillChoice>? Skills,
    ProfileAvailability? Availability);

public sealed record MentorWorkplaceView(
    Guid Id, string Company, string Title, int StartYear, int? EndYear, bool IsCurrent)
{
    public static IReadOnlyList<MentorWorkplaceView> From(IEnumerable<MentorWorkplace> workplaces) =>
        workplaces.OrderBy(workplace => workplace.SortOrder)
            .Select(workplace => new MentorWorkplaceView(
                workplace.Id, workplace.Company, workplace.Title, workplace.StartYear, workplace.EndYear, workplace.IsCurrent))
            .ToArray();
}

public sealed record MentorProfileView(
    Guid UserId,
    PersonalView Personal,
    Guid PositionId,
    Guid ExperienceId,
    IReadOnlyList<MentorSkillChoice> OwnedSkills,
    IReadOnlyList<MentorSkillChoice> Skills,
    IReadOnlyList<Guid> DirectionIds,
    Guid FormatId,
    Guid AvailabilityId,
    ProfileAvailability? Availability,
    CatalogRef? MeetingCity,
    Guid PricingModelId,
    decimal Price,
    bool HasFreeTrial,
    IReadOnlyList<MentorWorkplaceView> Workplaces,
    DateTimeOffset UpdatedAt)
{
    public static MentorProfileView From(UserProfile personal, MentorProfile mentor, Uri? photoUrl, CatalogRef? meetingCity) =>
        new(
            mentor.UserId,
            PersonalView.From(personal, photoUrl),
            mentor.PositionId,
            mentor.ExperienceId,
            mentor.OwnedSkills.Select(skill => new MentorSkillChoice(skill.SkillId, skill.SkillLevelId)).OrderBy(skill => skill.SkillId).ToArray(),
            mentor.Skills.Select(skill => new MentorSkillChoice(skill.SkillId, skill.SkillLevelId)).OrderBy(skill => skill.SkillId).ToArray(),
            mentor.Directions.Select(direction => direction.DirectionId).Order().ToList(),
            mentor.FormatId,
            mentor.AvailabilityId,
            mentor.Availability,
            meetingCity,
            mentor.PricingModelId,
            mentor.Price,
            mentor.HasFreeTrial,
            MentorWorkplaceView.From(mentor.Workplaces),
            personal.UpdatedAt);
}

public abstract record SaveMentorProfileResult
{
    public sealed record Saved(MentorProfileView Profile) : SaveMentorProfileResult;

    public sealed record AccountNotFound : SaveMentorProfileResult;

    public sealed record RoleConflict : SaveMentorProfileResult;

    public sealed record Forbidden : SaveMentorProfileResult;
}
