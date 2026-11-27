using API.Profiles;
using Application.Mentors;

namespace API.Mentors;

public sealed record MentorSkillResponse(Guid SkillId, Guid SkillLevelId);

public sealed record MentorProfileResponse(
    Guid UserId,
    PersonalInfoResponse Personal,
    Guid PositionId,
    Guid ExperienceId,
    IReadOnlyList<MentorSkillResponse> OwnedSkills,
    IReadOnlyList<MentorSkillResponse> Skills,
    IReadOnlyList<Guid> DirectionIds,
    Guid FormatId,
    Guid AvailabilityId,
    Guid PricingModelId,
    string Price,
    bool HasFreeTrial,
    IReadOnlyList<MentorWorkplaceView> Workplaces,
    DateTimeOffset UpdatedAt)
{
    public static MentorProfileResponse From(MentorProfileView profile) =>
        new(
            profile.UserId,
            PersonalInfoResponse.From(profile.Personal),
            profile.PositionId,
            profile.ExperienceId,
            profile.OwnedSkills.Select(skill => new MentorSkillResponse(skill.SkillId, skill.SkillLevelId)).ToArray(),
            profile.Skills.Select(skill => new MentorSkillResponse(skill.SkillId, skill.SkillLevelId)).ToArray(),
            profile.DirectionIds,
            profile.FormatId,
            profile.AvailabilityId,
            profile.PricingModelId,
            Money.Format(profile.Price)!,
            profile.HasFreeTrial,
            profile.Workplaces,
            profile.UpdatedAt);
}
