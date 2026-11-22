using Domain.Common;
using Domain.Mentees;

namespace UnitTests.Domain;

public class MenteeProfileTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid SkillA = Guid.NewGuid();
    private static readonly Guid SkillB = Guid.NewGuid();
    private static readonly Guid SkillC = Guid.NewGuid();
    private static readonly Guid Task = Guid.NewGuid();
    private const string Goal = "Освоить ASP.NET Core и собрать учебный API";

    private static MenteeAnswers Answers(
        Guid[]? skillIds = null,
        Guid[]? taskIds = null,
        decimal? budgetFrom = null,
        decimal? budgetTo = null,
        string goal = Goal) =>
        new(skillIds ?? [SkillA], Guid.NewGuid(), taskIds ?? [Task], Guid.NewGuid(), Guid.NewGuid(), budgetFrom, budgetTo, goal, Guid.NewGuid());

    [Fact]
    public void Constructor_StoresAnswersAndCollections()
    {
        var profile = new MenteeProfile(UserId, Answers([SkillA, SkillB], budgetFrom: 1000m, budgetTo: 2000.50m, goal: $"  {Goal}  "));

        Assert.Equal(Goal, profile.Goal);
        Assert.Equal(1000m, profile.BudgetFrom);
        Assert.Equal(2000.50m, profile.BudgetTo);
        Assert.Equal([SkillA, SkillB], profile.Skills.Select(skill => skill.SkillId));
        Assert.All(profile.Skills, skill => Assert.Equal(UserId, skill.MenteeId));
        Assert.Equal([Task], profile.Tasks.Select(task => task.TaskId));
    }

    [Fact]
    public void Update_KeepsUnchangedItemsAndReplacesOthers()
    {
        var profile = new MenteeProfile(UserId, Answers([SkillA, SkillB]));
        var keptSkill = profile.Skills.Single(skill => skill.SkillId == SkillB);

        profile.Update(Answers([SkillB, SkillC]));

        Assert.Equal([SkillB, SkillC], profile.Skills.Select(skill => skill.SkillId));
        Assert.Same(keptSkill, profile.Skills.First());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10.555)]
    [InlineData(10_000_000_000)]
    public void Answers_RejectInvalidBudget(double budget)
    {
        var exception = Assert.Throws<DomainException>(() => Answers(budgetFrom: (decimal)budget));

        Assert.Equal(nameof(MenteeAnswers.BudgetFrom), exception.Field);
    }

    [Fact]
    public void Answers_RejectBudgetToBelowBudgetFrom()
    {
        var exception = Assert.Throws<DomainException>(() => Answers(budgetFrom: 2000m, budgetTo: 1000m));

        Assert.Equal(nameof(MenteeAnswers.BudgetTo), exception.Field);
    }

    [Fact]
    public void Answers_AllowOnlyOneBudgetBound()
    {
        var answers = Answers(budgetTo: 500m);

        Assert.Null(answers.BudgetFrom);
        Assert.Equal(500m, answers.BudgetTo);
    }

    [Theory]
    [InlineData("Коротко")]
    [InlineData("                         ")]
    public void Answers_RejectShortGoal(string goal)
    {
        var exception = Assert.Throws<DomainException>(() => Answers(goal: goal));

        Assert.Equal(nameof(MenteeAnswers.Goal), exception.Field);
    }

    [Fact]
    public void Answers_RejectDuplicateSkills()
    {
        var exception = Assert.Throws<DomainException>(() => Answers([SkillA, SkillA]));

        Assert.Equal(nameof(MenteeAnswers.SkillIds), exception.Field);
    }

    [Fact]
    public void Answers_RejectEmptyTasks()
    {
        var exception = Assert.Throws<DomainException>(() => Answers(taskIds: []));

        Assert.Equal(nameof(MenteeAnswers.TaskIds), exception.Field);
    }
}
