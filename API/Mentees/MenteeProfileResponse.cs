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
    Guid Id, string Text, LookupResponse Deadline, IReadOnlyList<LookupResponse> Directions);

public sealed record MenteeProfileResponse(
    Guid UserId,
    MenteePersonalResponse Personal,
    IReadOnlyList<MenteeOwnedSkillResponse> OwnedSkills,
    IReadOnlyList<LookupResponse> Skills,
    LookupResponse SkillLevel,
    IReadOnlyList<LookupResponse> Tasks,
    LookupResponse Format,
    LookupResponse WeeklyAvailability,
    MenteeAvailabilityResponse? Availability,
    string? BudgetFrom,
    string? BudgetTo,
    string Goal,
    LookupResponse GoalDeadline,
    IReadOnlyList<MenteeGoalResponse> Goals,
    DateTimeOffset UpdatedAt)
{
    public static MenteeProfileResponse From(MenteeProfileView profile)
    {
        LookupResponse Lookup(Guid id) => LookupResponse.From(profile.CatalogEntries[id]);

        var meetingCity = profile.Availability?.MeetingCityId is { } cityId ? Lookup(cityId) : null;
        var ownedSkills = profile.OwnedSkills.Select(skill =>
            new MenteeOwnedSkillResponse(Lookup(skill.SkillId), Lookup(skill.SkillLevelId)));
        var goals = profile.Goals.Select(goal =>
            new MenteeGoalResponse(goal.Id, goal.Text, Lookup(goal.DeadlineId), goal.DirectionIds.Select(Lookup).ToArray()));

        return new(
            profile.UserId,
            MenteePersonalResponse.From(profile),
            ownedSkills.ToArray(),
            profile.SkillIds.Select(Lookup).ToArray(),
            Lookup(profile.SkillLevelId),
            profile.TaskIds.Select(Lookup).ToArray(),
            Lookup(profile.FormatId),
            Lookup(profile.AvailabilityId),
            MenteeAvailabilityResponse.From(profile.Availability, meetingCity),
            Money.Format(profile.BudgetFrom),
            Money.Format(profile.BudgetTo),
            profile.Goal,
            Lookup(profile.GoalDeadlineId),
            goals.ToArray(),
            profile.UpdatedAt);
    }
}
