using API.Profiles;
using Application.Mentees;

namespace API.Mentees;

public sealed record MenteeProfileResponse(
    Guid UserId,
    PersonalInfoResponse Personal,
    IReadOnlyList<Guid> SkillIds,
    Guid SkillLevelId,
    IReadOnlyList<Guid> TaskIds,
    Guid FormatId,
    Guid AvailabilityId,
    string? BudgetFrom,
    string? BudgetTo,
    string Goal,
    Guid GoalDeadlineId,
    DateTimeOffset UpdatedAt)
{
    public static MenteeProfileResponse From(MenteeProfileView profile) =>
        new(
            profile.UserId,
            PersonalInfoResponse.From(profile.Personal),
            profile.SkillIds,
            profile.SkillLevelId,
            profile.TaskIds,
            profile.FormatId,
            profile.AvailabilityId,
            Money.Format(profile.BudgetFrom),
            Money.Format(profile.BudgetTo),
            profile.Goal,
            profile.GoalDeadlineId,
            profile.UpdatedAt);
}
