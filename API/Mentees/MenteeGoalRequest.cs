using System.ComponentModel.DataAnnotations;
using Domain.Common;
using Domain.Mentees;

namespace API.Mentees;

public sealed class MenteeGoalRequest
{
    public Guid? Id { get; init; }

    [Required, StringLength(MenteeAnswers.GoalMaxLength, MinimumLength = MenteeAnswers.GoalMinLength)]
    public string Text { get; init; } = null!;

    [Required]
    public Guid? DeadlineId { get; init; }

    [Required, MaxLength(MenteeGoal.MaxDirections)]
    public Guid[] DirectionIds { get; init; } = null!;

    public static MenteeGoalChoice[] ToChoices(IEnumerable<MenteeGoalRequest?> goals) => goals
        .Select((goal, index) => goal?.ToChoice()
            ?? throw new DomainException($"Goals[{index}]", "Укажите цель."))
        .ToArray();

    private MenteeGoalChoice ToChoice() => new(Id, Text, DeadlineId!.Value, DirectionIds);
}
