using Domain.Common;

namespace Domain.Mentees;

public sealed class MenteeAnswers
{
    public const int MaxSkills = 50;
    public const int MaxOwnedSkills = 50;
    public const int MaxTasks = 20;
    public const int GoalMinLength = 20;
    public const int GoalMaxLength = 2000;

    public MenteeAnswers(
        IEnumerable<Guid> skillIds,
        IReadOnlyList<MenteeOwnedSkillChoice> ownedSkills,
        IEnumerable<Guid> taskIds,
        Guid formatId,
        Guid availabilityId,
        decimal? budgetFrom,
        decimal? budgetTo,
        IReadOnlyList<MenteeGoalChoice> goals)
    {
        SkillIds = Require.Ids(skillIds, 1, MaxSkills, nameof(SkillIds));
        ValidateOwnedSkills(ownedSkills, minCount: 1);
        OwnedSkills = ownedSkills.ToArray();
        TaskIds = Require.Ids(taskIds, 1, MaxTasks, nameof(TaskIds));
        FormatId = Require.Id(formatId, nameof(FormatId));
        AvailabilityId = Require.Id(availabilityId, nameof(AvailabilityId));
        BudgetFrom = budgetFrom is { } from ? Require.Amount(from, nameof(BudgetFrom)) : null;
        BudgetTo = budgetTo is { } to ? Require.Amount(to, nameof(BudgetTo)) : null;
        if (goals.Count is < 1 or > MenteeProfile.MaxGoals)
        {
            throw new DomainException(nameof(Goals), $"Укажите от 1 до {MenteeProfile.MaxGoals} целей.");
        }

        Goals = goals.ToArray();

        if (BudgetFrom > BudgetTo)
        {
            throw new DomainException(nameof(BudgetTo), "Бюджет «до» не может быть меньше бюджета «от».");
        }
    }

    public IReadOnlyList<Guid> SkillIds { get; }

    public IReadOnlyList<MenteeOwnedSkillChoice> OwnedSkills { get; }

    public IReadOnlyList<Guid> TaskIds { get; }

    public Guid FormatId { get; }

    public Guid AvailabilityId { get; }

    public decimal? BudgetFrom { get; }

    public decimal? BudgetTo { get; }

    public IReadOnlyList<MenteeGoalChoice> Goals { get; }

    internal static void ValidateOwnedSkills(IReadOnlyList<MenteeOwnedSkillChoice> ownedSkills, int minCount)
    {
        Require.Ids(ownedSkills.Select(skill => skill.SkillId), minCount, MaxOwnedSkills, nameof(OwnedSkills));
        if (ownedSkills.Any(skill => skill.SkillLevelId == Guid.Empty))
        {
            throw new DomainException(nameof(OwnedSkills), "Для каждого навыка выберите уровень.");
        }
    }
}
