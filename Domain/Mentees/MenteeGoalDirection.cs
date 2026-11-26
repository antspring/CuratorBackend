namespace Domain.Mentees;

public sealed class MenteeGoalDirection
{
    private MenteeGoalDirection()
    {
    }

    internal MenteeGoalDirection(Guid menteeId, Guid goalId, Guid directionId)
    {
        MenteeId = menteeId;
        GoalId = goalId;
        DirectionId = directionId;
    }

    public Guid MenteeId { get; private set; }

    public Guid GoalId { get; private set; }

    public Guid DirectionId { get; private set; }
}
