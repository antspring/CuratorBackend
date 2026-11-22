using Domain.Common;

namespace Domain.Mentees;

public sealed class MenteeAnswers
{
    public const int MaxSkills = 50;
    public const int MaxTasks = 20;
    public const int GoalMinLength = 20;
    public const int GoalMaxLength = 2000;

    public MenteeAnswers(
        IEnumerable<Guid> skillIds,
        Guid skillLevelId,
        IEnumerable<Guid> taskIds,
        Guid formatId,
        Guid availabilityId,
        decimal? budgetFrom,
        decimal? budgetTo,
        string goal,
        Guid goalDeadlineId)
    {
        SkillIds = Require.Ids(skillIds, 1, MaxSkills, nameof(SkillIds));
        SkillLevelId = Require.Id(skillLevelId, nameof(SkillLevelId));
        TaskIds = Require.Ids(taskIds, 1, MaxTasks, nameof(TaskIds));
        FormatId = Require.Id(formatId, nameof(FormatId));
        AvailabilityId = Require.Id(availabilityId, nameof(AvailabilityId));
        BudgetFrom = budgetFrom is { } from ? Require.Amount(from, nameof(BudgetFrom)) : null;
        BudgetTo = budgetTo is { } to ? Require.Amount(to, nameof(BudgetTo)) : null;
        Goal = Require.Text(goal, GoalMinLength, GoalMaxLength, nameof(Goal));
        GoalDeadlineId = Require.Id(goalDeadlineId, nameof(GoalDeadlineId));

        if (BudgetFrom > BudgetTo)
        {
            throw new DomainException(nameof(BudgetTo), "Бюджет «до» не может быть меньше бюджета «от».");
        }
    }

    public IReadOnlyList<Guid> SkillIds { get; }

    public Guid SkillLevelId { get; }

    public IReadOnlyList<Guid> TaskIds { get; }

    public Guid FormatId { get; }

    public Guid AvailabilityId { get; }

    public decimal? BudgetFrom { get; }

    public decimal? BudgetTo { get; }

    public string Goal { get; }

    public Guid GoalDeadlineId { get; }
}
