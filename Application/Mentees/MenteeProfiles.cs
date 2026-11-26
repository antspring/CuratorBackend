using Domain.Mentees;
using Application.Profiles;
using Domain.Users;

namespace Application.Mentees;

public sealed record SaveMenteePersonalCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    int Age,
    Guid CityId,
    Guid? PhotoFileId,
    string Status,
    string University,
    string? Specialty,
    string? AboutMe);

public sealed record SaveMenteeProfileCommand(
    Guid UserId,
    PersonalInput Personal,
    IReadOnlyList<Guid> SkillIds,
    Guid SkillLevelId,
    IReadOnlyList<Guid> TaskIds,
    Guid FormatId,
    Guid AvailabilityId,
    decimal? BudgetFrom,
    decimal? BudgetTo,
    string Goal,
    Guid GoalDeadlineId);

public sealed record MenteeProfileView(
    Guid UserId,
    PersonalView Personal,
    string? Status,
    string? University,
    string? Specialty,
    string? AboutMe,
    IReadOnlyList<MenteeOwnedSkillChoice> OwnedSkills,
    IReadOnlyList<Guid> SkillIds,
    Guid SkillLevelId,
    IReadOnlyList<Guid> TaskIds,
    Guid FormatId,
    Guid AvailabilityId,
    decimal? BudgetFrom,
    decimal? BudgetTo,
    string Goal,
    Guid GoalDeadlineId,
    DateTimeOffset UpdatedAt)
{
    public static MenteeProfileView From(UserProfile personal, MenteeProfile mentee, Uri? photoUrl) =>
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
            mentee.SkillLevelId,
            mentee.Tasks.Select(task => task.TaskId).Order().ToList(),
            mentee.FormatId,
            mentee.AvailabilityId,
            mentee.BudgetFrom,
            mentee.BudgetTo,
            mentee.Goal,
            mentee.GoalDeadlineId,
            personal.UpdatedAt);
}

public abstract record SaveMenteeProfileResult
{
    public sealed record Saved(MenteeProfileView Profile) : SaveMenteeProfileResult;

    public sealed record AccountNotFound : SaveMenteeProfileResult;

    public sealed record RoleConflict : SaveMenteeProfileResult;

    public sealed record Forbidden : SaveMenteeProfileResult;
}
