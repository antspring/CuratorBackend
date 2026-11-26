using API.Profiles;
using Application.Mentees;
using Domain.Mentees;

namespace API.Mentees;

public sealed record MenteePersonalResponse(
    string FirstName, string LastName, int Age, Guid CityId, Guid LanguageId, Guid? PhotoFileId, Uri? PhotoUrl,
    string? Status, string? University, string? Specialty, string? AboutMe)
{
    public static MenteePersonalResponse From(MenteeProfileView profile)
    {
        var personal = profile.Personal;
        return new(personal.FirstName, personal.LastName, personal.Age, personal.CityId, personal.LanguageId,
            personal.PhotoFileId, personal.PhotoUrl, profile.Status, profile.University, profile.Specialty, profile.AboutMe);
    }
}

public sealed record MenteeProfileResponse(
    Guid UserId,
    MenteePersonalResponse Personal,
    IReadOnlyList<MenteeOwnedSkillChoice> OwnedSkills,
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
            MenteePersonalResponse.From(profile),
            profile.OwnedSkills,
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
