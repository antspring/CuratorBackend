namespace Domain.Mentors;

public sealed class MentorDirection
{
    private MentorDirection()
    {
    }

    internal MentorDirection(Guid mentorId, Guid directionId)
    {
        MentorId = mentorId;
        DirectionId = directionId;
    }

    public Guid MentorId { get; private set; }

    public Guid DirectionId { get; private set; }
}
