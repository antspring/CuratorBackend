using Domain.Common;

namespace Domain.Mentees;

public sealed record MenteeGoalChoice(Guid Id, string Text, Guid DeadlineId, IReadOnlyList<Guid> DirectionIds);

public sealed class MenteeGoal
{
    public const int MaxDirections = 20;

    private readonly List<MenteeGoalDirection> directions = [];

    private MenteeGoal()
    {
    }

    internal MenteeGoal(Guid menteeId, Guid id)
    {
        MenteeId = menteeId;
        Id = id;
    }

    public Guid MenteeId { get; private set; }

    public Guid Id { get; private set; }

    public string Text { get; private set; } = null!;

    public Guid DeadlineId { get; private set; }

    public int SortOrder { get; private set; }

    public IReadOnlyCollection<MenteeGoalDirection> Directions => directions;

    internal void Update(MenteeGoalChoice choice, int sortOrder, string field)
    {
        var text = Require.Text(choice.Text, MenteeAnswers.GoalMinLength, MenteeAnswers.GoalMaxLength, $"{field}.Text");
        var deadlineId = Require.Id(choice.DeadlineId, $"{field}.DeadlineId");
        var unchangedQuestionnaireGoal = Directions.Count == 0 && Text == text && DeadlineId == deadlineId;
        var directionIds = Require.Ids(choice.DirectionIds, unchangedQuestionnaireGoal ? 0 : 1, MaxDirections, $"{field}.DirectionIds");

        Text = text;
        DeadlineId = deadlineId;
        SortOrder = sortOrder;
        directions.RemoveAll(direction => !directionIds.Contains(direction.DirectionId));
        var existing = directions.Select(direction => direction.DirectionId).ToHashSet();
        directions.AddRange(directionIds.Where(id => !existing.Contains(id))
            .Select(id => new MenteeGoalDirection(MenteeId, Id, id)));
    }

    internal void SetQuestionnaireGoal(string text, Guid deadlineId)
    {
        Text = text;
        DeadlineId = deadlineId;
    }
}
