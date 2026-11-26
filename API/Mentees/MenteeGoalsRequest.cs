using System.ComponentModel.DataAnnotations;
using Domain.Common;
using Domain.Mentees;

namespace API.Mentees;

public sealed class MenteeGoalsRequest
{
    [Required, MaxLength(MenteeProfile.MaxGoals)]
    public MenteeGoalRequest?[] Goals { get; init; } = null!;

    public MenteeGoalChoice[] ToChoices() => Goals
        .Select((goal, index) => goal?.ToChoice() ?? throw new DomainException($"Goals[{index}]", "Укажите цель."))
        .ToArray();
}

public sealed class MenteeGoalRequest
{
    [Required]
    public Guid? Id { get; init; }

    [Required, StringLength(MenteeAnswers.GoalMaxLength, MinimumLength = MenteeAnswers.GoalMinLength)]
    public string Text { get; init; } = null!;

    [Required]
    public Guid? DeadlineId { get; init; }

    [Required, MaxLength(MenteeGoal.MaxDirections)]
    public Guid[] DirectionIds { get; init; } = null!;

    public MenteeGoalChoice ToChoice() => new(Id!.Value, Text, DeadlineId!.Value, DirectionIds);
}
