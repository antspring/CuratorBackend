using Domain.Common;

namespace Domain.Mentees;

public sealed record MenteeGoalChoice(Guid? Id, string Text, Guid DeadlineId, IReadOnlyList<Guid> DirectionIds);

public sealed class MenteeGoal
{
    public const int MaxDirections = 20;

    private readonly List<MenteeGoalDirection> directions = [];

    private MenteeGoal()
    {
    }

    internal MenteeGoal(Guid menteeId)
    {
        MenteeId = menteeId;
        Id = Guid.CreateVersion7();
    }

    public Guid MenteeId { get; private set; }

    public Guid Id { get; private set; }

    public string Text { get; private set; } = null!;

    public Guid DeadlineId { get; private set; }

    public int SortOrder { get; private set; }

    public IReadOnlyCollection<MenteeGoalDirection> Directions => directions;

    internal bool Matches(MenteeGoalChoice choice) =>
        Text == choice.Text.Trim() && DeadlineId == choice.DeadlineId &&
        directions.Count == choice.DirectionIds.Count &&
        directions.All(direction => choice.DirectionIds.Contains(direction.DirectionId));

    internal void Update(MenteeGoalChoice choice, int sortOrder, string field, bool allowMissingDirections)
    {
        var text = Require.Text(choice.Text, MenteeAnswers.GoalMinLength, MenteeAnswers.GoalMaxLength, $"{field}.Text");
        var deadlineId = Require.Id(choice.DeadlineId, $"{field}.DeadlineId");
        var unchangedQuestionnaireGoal = Directions.Count == 0 && Text == text && DeadlineId == deadlineId;
        var directionIds = Require.Ids(choice.DirectionIds, allowMissingDirections || unchangedQuestionnaireGoal ? 0 : 1, MaxDirections, $"{field}.DirectionIds");

        Text = text;
        DeadlineId = deadlineId;
        SortOrder = sortOrder;
        directions.RemoveAll(direction => !directionIds.Contains(direction.DirectionId));
        var existing = directions.Select(direction => direction.DirectionId).ToHashSet();
        directions.AddRange(directionIds.Where(id => !existing.Contains(id))
            .Select(id => new MenteeGoalDirection(MenteeId, Id, id)));
    }
}
