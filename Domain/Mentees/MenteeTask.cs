namespace Domain.Mentees;

public sealed class MenteeTask
{
    private MenteeTask()
    {
    }

    internal MenteeTask(Guid menteeId, Guid taskId)
    {
        MenteeId = menteeId;
        TaskId = taskId;
    }

    public Guid MenteeId { get; private set; }

    public Guid TaskId { get; private set; }
}
