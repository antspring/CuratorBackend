using Domain.Common;

namespace Domain.Mentees;

public sealed class MenteeProfile
{
    private readonly List<MenteeSkill> skills = [];
    private readonly List<MenteeTask> tasks = [];

    private MenteeProfile()
    {
    }

    public MenteeProfile(Guid userId, MenteeAnswers answers)
    {
        UserId = Require.Id(userId, nameof(UserId));
        Update(answers);
    }

    public Guid UserId { get; private set; }

    public Guid SkillLevelId { get; private set; }

    public Guid FormatId { get; private set; }

    public Guid AvailabilityId { get; private set; }

    public decimal? BudgetFrom { get; private set; }

    public decimal? BudgetTo { get; private set; }

    public string Goal { get; private set; } = null!;

    public Guid GoalDeadlineId { get; private set; }

    public IReadOnlyCollection<MenteeSkill> Skills => skills;

    public IReadOnlyCollection<MenteeTask> Tasks => tasks;

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
