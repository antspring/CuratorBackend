using Domain.Common;
using Domain.Users;

namespace Domain.Mentees;

public sealed class MenteeProfile
{
    public const int StatusMaxLength = 100;
    public const int EducationMaxLength = 200;
    public const int AboutMeMaxLength = 300;
    public const int MaxGoals = 20;

    public string? Status { get; private set; }
    public string? University { get; private set; }
    public string? Specialty { get; private set; }
    public string? AboutMe { get; private set; }

    private readonly List<MenteeSkill> skills = [];
    private readonly List<MenteeTask> tasks = [];
    private readonly List<MenteeOwnedSkill> ownedSkills = [];
    private readonly List<MenteeGoal> goals = [];

    private MenteeProfile()
    {
    }

    public MenteeProfile(Guid userId, MenteeAnswers answers)
    {
        UserId = Require.Id(userId, nameof(UserId));
        Update(answers);
        ReplaceGoals(answers.Goals, allowMissingDirections: true);
    }

    public Guid UserId { get; private set; }

    public Guid FormatId { get; private set; }

    public Guid AvailabilityId { get; private set; }

    public ProfileAvailability? Availability { get; private set; }

    public decimal? BudgetFrom { get; private set; }

    public decimal? BudgetTo { get; private set; }

    public IReadOnlyCollection<MenteeSkill> Skills => skills;

    public IReadOnlyCollection<MenteeTask> Tasks => tasks;

    public IReadOnlyCollection<MenteeOwnedSkill> OwnedSkills => ownedSkills;

    public IReadOnlyCollection<MenteeGoal> Goals => goals;

    public void ReplaceGoals(IReadOnlyList<MenteeGoalChoice> choices) => ReplaceGoals(choices, allowMissingDirections: false);

    private void ReplaceGoals(IReadOnlyList<MenteeGoalChoice> choices, bool allowMissingDirections)
    {
        if (choices.Count > MaxGoals)
        {
            throw new DomainException(nameof(Goals), $"Можно указать до {MaxGoals} целей.");
        }

        var ids = Require.Ids(choices.Where(choice => choice.Id.HasValue).Select(choice => choice.Id!.Value),
            0, MaxGoals, nameof(Goals)).ToHashSet();
        var byId = goals.ToDictionary(goal => goal.Id);
        var unmatched = goals.Where(goal => !ids.Contains(goal.Id)).OrderBy(goal => goal.SortOrder).ToList();

        for (var index = 0; index < choices.Count; index++)
        {
            var choice = choices[index];
            var field = $"Goals[{index}]";
            var goal = choice.Id is { } id
                ? byId.GetValueOrDefault(id) ?? throw new DomainException($"{field}.Id", "Цель не найдена в вашем профиле.")
                : unmatched.Find(candidate => candidate.Matches(choice));

            if (goal is null)
            {
                goal = new MenteeGoal(UserId);
                goal.Update(choice, index, field, allowMissingDirections);
                goals.Add(goal);
            }
            else
            {
                goal.Update(choice, index, field, allowMissingDirections);
                unmatched.Remove(goal);
            }
        }

        goals.RemoveAll(unmatched.Contains);
    }

    public void ReplaceOwnedSkills(IReadOnlyList<MenteeOwnedSkillChoice> choices)
    {
        MenteeAnswers.ValidateOwnedSkills(choices, minCount: 0);
        SyncOwnedSkills(choices);
    }

    private void SyncOwnedSkills(IReadOnlyList<MenteeOwnedSkillChoice> choices)
    {
        var byId = choices.ToDictionary(choice => choice.SkillId);

        Sync(ownedSkills, byId.Keys.ToArray(), skill => skill.SkillId, skillId => new MenteeOwnedSkill(UserId, byId[skillId]));
        foreach (var skill in ownedSkills)
        {
            skill.SkillLevelId = byId[skill.SkillId].SkillLevelId;
        }
    }

    public void UpdateAvailability(ProfileAvailability availability) => Availability = availability;

    public void UpdatePersonalDetails(string status, string university, string? specialty, string? aboutMe)
    {
        Status = Require.Text(status, 1, StatusMaxLength, nameof(Status));
        University = Require.Text(university, 1, EducationMaxLength, nameof(University));
        Specialty = string.IsNullOrWhiteSpace(specialty) ? null : Require.Text(specialty, 1, EducationMaxLength, nameof(Specialty));
        AboutMe = string.IsNullOrWhiteSpace(aboutMe) ? null : Require.Text(aboutMe, 1, AboutMeMaxLength, nameof(AboutMe));
    }

    public void Update(MenteeAnswers answers)
    {
        ArgumentNullException.ThrowIfNull(answers);

        FormatId = answers.FormatId;
        AvailabilityId = answers.AvailabilityId;
        BudgetFrom = answers.BudgetFrom;
        BudgetTo = answers.BudgetTo;

        Sync(skills, answers.SkillIds, skill => skill.SkillId, skillId => new MenteeSkill(UserId, skillId));
        SyncOwnedSkills(answers.OwnedSkills);
        Sync(tasks, answers.TaskIds, task => task.TaskId, taskId => new MenteeTask(UserId, taskId));
    }

    private static void Sync<T>(List<T> current, IReadOnlyList<Guid> ids, Func<T, Guid> key, Func<Guid, T> create)
    {
        current.RemoveAll(item => !ids.Contains(key(item)));

        var existing = current.Select(key).ToHashSet();
        current.AddRange(ids.Where(id => !existing.Contains(id)).Select(create));
    }
}
