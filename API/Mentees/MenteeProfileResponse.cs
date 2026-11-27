using API.Common;
using API.Profiles;
using Application.Mentees;

namespace API.Mentees;

public sealed record MenteePersonalResponse(
    string FirstName, string LastName, int Age, LookupResponse City, LookupResponse Language,
    Guid? PhotoFileId, Uri? PhotoUrl, string? Status, string? University, string? Specialty, string? AboutMe)
{
    public static MenteePersonalResponse From(MenteeProfileView profile)
    {
        var personal = profile.Personal;
        return new(personal.FirstName, personal.LastName, personal.Age,
            LookupResponse.From(profile.CatalogEntries[personal.CityId]),
            LookupResponse.From(profile.CatalogEntries[personal.LanguageId]),
            personal.PhotoFileId, personal.PhotoUrl, profile.Status, profile.University, profile.Specialty, profile.AboutMe);
    }
}

public sealed record MenteeOwnedSkillResponse(LookupResponse Skill, LookupResponse Level);

public sealed record MenteeGoalResponse(
    Guid Id, string Text, LookupResponse Deadline, IReadOnlyList<LookupResponse> Directions)
{
    public static MenteeGoalResponse From(MenteeGoalView goal) =>
        new(goal.Id, goal.Text, LookupResponse.From(goal.Deadline), goal.Directions.Select(LookupResponse.From).ToArray());
}

public sealed record MenteeProfileResponse(
    Guid UserId,
    MenteePersonalResponse Personal,
    IReadOnlyList<MenteeOwnedSkillResponse> OwnedSkills,
    IReadOnlyList<LookupResponse> Skills,
    IReadOnlyList<LookupResponse> Tasks,
    LookupResponse Format,
    LookupResponse WeeklyAvailability,
    MenteeAvailabilityResponse? Availability,
    string? BudgetFrom,
    string? BudgetTo,
    IReadOnlyList<MenteeGoalResponse> Goals,
    DateTimeOffset UpdatedAt)
{
    public static MenteeProfileResponse From(MenteeProfileView profile)
    {
        LookupResponse Lookup(Guid id) => LookupResponse.From(profile.CatalogEntries[id]);

        var meetingCity = profile.Availability?.MeetingCityId is { } cityId ? Lookup(cityId) : null;
        var ownedSkills = profile.OwnedSkills.Select(skill =>
            new MenteeOwnedSkillResponse(Lookup(skill.SkillId), Lookup(skill.SkillLevelId)));

        return new(
            profile.UserId,
            MenteePersonalResponse.From(profile),
            ownedSkills.ToArray(),
            profile.SkillIds.Select(Lookup).ToArray(),
            profile.TaskIds.Select(Lookup).ToArray(),
            Lookup(profile.FormatId),
            Lookup(profile.AvailabilityId),
            MenteeAvailabilityResponse.From(profile.Availability, meetingCity),
            Money.Format(profile.BudgetFrom),
            Money.Format(profile.BudgetTo),
            profile.Goals.Select(MenteeGoalResponse.From).ToArray(),
            profile.UpdatedAt);
    }
}
