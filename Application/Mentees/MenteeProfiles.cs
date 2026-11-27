using Application.Catalogs;
using Application.Profiles;
using Domain.Mentees;
using Domain.Users;

namespace Application.Mentees;

public sealed record MenteePersonalInput(
    string FirstName,
    string LastName,
    int Age,
    Guid CityId,
    Guid? PhotoFileId,
    string Status,
    string University,
    string? Specialty,
    string? AboutMe);

public sealed record MenteeProfilePatch(
    MenteePersonalInput? Personal,
    IReadOnlyList<MenteeOwnedSkillChoice>? OwnedSkills,
    IReadOnlyList<MenteeGoalChoice>? Goals,
    ProfileAvailability? Availability);

public sealed record SaveMenteeProfileCommand(
    Guid UserId,
    PersonalInput Personal,
    IReadOnlyList<Guid> SkillIds,
    IReadOnlyList<MenteeOwnedSkillChoice> OwnedSkills,
    IReadOnlyList<Guid> TaskIds,
    Guid FormatId,
    Guid AvailabilityId,
    decimal? BudgetFrom,
    decimal? BudgetTo,
    IReadOnlyList<MenteeGoalChoice> Goals);

public sealed record MenteeGoalView(Guid Id, string Text, CatalogRef Deadline, IReadOnlyList<CatalogRef> Directions)
{
    public static IReadOnlyList<MenteeGoalView> From(
        IEnumerable<MenteeGoal> goals, IReadOnlyDictionary<Guid, CatalogRef> catalogEntries) =>
        goals.OrderBy(goal => goal.SortOrder)
            .Select(goal => new MenteeGoalView(goal.Id, goal.Text, catalogEntries[goal.DeadlineId],
                goal.Directions.Select(direction => direction.DirectionId).Order().Select(id => catalogEntries[id]).ToArray()))
            .ToArray();
}

public sealed record MenteeProfileView(
    Guid UserId,
    PersonalView Personal,
    string? Status,
    string? University,
    string? Specialty,
    string? AboutMe,
    IReadOnlyList<MenteeOwnedSkillChoice> OwnedSkills,
    IReadOnlyList<Guid> SkillIds,
    IReadOnlyList<Guid> TaskIds,
    Guid FormatId,
    Guid AvailabilityId,
    ProfileAvailability? Availability,
    decimal? BudgetFrom,
    decimal? BudgetTo,
    IReadOnlyList<MenteeGoalView> Goals,
    DateTimeOffset UpdatedAt,
    IReadOnlyDictionary<Guid, CatalogRef> CatalogEntries)
{
    public static MenteeProfileView From(
        UserProfile personal, MenteeProfile mentee, Uri? photoUrl, IReadOnlyDictionary<Guid, CatalogRef> catalogEntries) =>
        new(
            mentee.UserId,
            PersonalView.From(personal, photoUrl),
            mentee.Status,
            mentee.University,
            mentee.Specialty,
            mentee.AboutMe,
            mentee.OwnedSkills.OrderBy(skill => skill.SkillId)
                .Select(skill => new MenteeOwnedSkillChoice(skill.SkillId, skill.SkillLevelId)).ToArray(),
            mentee.Skills.Select(skill => skill.SkillId).Order().ToList(),
            mentee.Tasks.Select(task => task.TaskId).Order().ToList(),
            mentee.FormatId,
            mentee.AvailabilityId,
            mentee.Availability,
            mentee.BudgetFrom,
            mentee.BudgetTo,
            MenteeGoalView.From(mentee.Goals, catalogEntries),
            personal.UpdatedAt,
            catalogEntries);
}

public abstract record SaveMenteeProfileResult
{
    public sealed record Saved(MenteeProfileView Profile) : SaveMenteeProfileResult;

    public sealed record AccountNotFound : SaveMenteeProfileResult;

    public sealed record RoleConflict : SaveMenteeProfileResult;

    public sealed record Forbidden : SaveMenteeProfileResult;
}
