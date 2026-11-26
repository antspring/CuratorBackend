using Domain.Common;

namespace Domain.Mentees;

public sealed class MenteeProfile
{
    public const int StatusMaxLength = 100;
    public const int EducationMaxLength = 200;
    public const int AboutMeMaxLength = 300;
    public const int MaxOwnedSkills = 50;
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
        var firstGoal = new MenteeGoal(UserId, Guid.CreateVersion7());
        firstGoal.SetQuestionnaireGoal(Goal, GoalDeadlineId);
        goals.Add(firstGoal);
    }

    public Guid UserId { get; private set; }

    public Guid SkillLevelId { get; private set; }

    public Guid FormatId { get; private set; }

    public Guid AvailabilityId { get; private set; }

    public MenteeAvailability? Availability { get; private set; }

    public decimal? BudgetFrom { get; private set; }

    public decimal? BudgetTo { get; private set; }

    public string Goal { get; private set; } = null!;

    public Guid GoalDeadlineId { get; private set; }

    public IReadOnlyCollection<MenteeSkill> Skills => skills;

    public IReadOnlyCollection<MenteeTask> Tasks => tasks;

    public IReadOnlyCollection<MenteeOwnedSkill> OwnedSkills => ownedSkills;

    public IReadOnlyCollection<MenteeGoal> Goals => goals;

    public void ReplaceGoals(IReadOnlyList<MenteeGoalChoice> choices)
    {
        var ids = Require.Ids(choices.Select(choice => choice.Id), 0, MaxGoals, nameof(Goals));
        Sync(goals, ids, goal => goal.Id, id => new MenteeGoal(UserId, id));

        var byId = goals.ToDictionary(goal => goal.Id);
        for (var index = 0; index < choices.Count; index++)
        {
            var choice = choices[index];
            byId[choice.Id].Update(choice, index, $"Goals[{index}]");
        }
    }

    public void ReplaceOwnedSkills(IReadOnlyList<MenteeOwnedSkillChoice> choices)
    {
        var ids = Require.Ids(choices.Select(choice => choice.SkillId), 0, MaxOwnedSkills, nameof(OwnedSkills));
        if (choices.Any(choice => choice.SkillLevelId == Guid.Empty))
        {
            throw new DomainException(nameof(OwnedSkills), "Для каждого навыка выберите уровень.");
        }

        var byId = choices.ToDictionary(choice => choice.SkillId);

        Sync(ownedSkills, ids, skill => skill.SkillId, skillId => new MenteeOwnedSkill(UserId, byId[skillId]));
        foreach (var skill in ownedSkills)
        {
            skill.SkillLevelId = byId[skill.SkillId].SkillLevelId;
        }
    }

    public void UpdateAvailability(MenteeAvailability availability) => Availability = availability;

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

        SkillLevelId = answers.SkillLevelId;
        FormatId = answers.FormatId;
        AvailabilityId = answers.AvailabilityId;
        BudgetFrom = answers.BudgetFrom;
        BudgetTo = answers.BudgetTo;
        Goal = answers.Goal;
        GoalDeadlineId = answers.GoalDeadlineId;

        Sync(skills, answers.SkillIds, skill => skill.SkillId, skillId => new MenteeSkill(UserId, skillId));
        Sync(tasks, answers.TaskIds, task => task.TaskId, taskId => new MenteeTask(UserId, taskId));
    }

    private static void Sync<T>(List<T> current, IReadOnlyList<Guid> ids, Func<T, Guid> key, Func<Guid, T> create)
    {
        current.RemoveAll(item => !ids.Contains(key(item)));

        var existing = current.Select(key).ToHashSet();
        current.AddRange(ids.Where(id => !existing.Contains(id)).Select(create));
    }
}
