using System.ComponentModel.DataAnnotations;
using Domain.Common;
using Domain.Mentors;

namespace API.Mentors;

public sealed class MentorProfilePatchRequest
{
    [Required, MaxLength(MentorProfile.MaxWorkplaces)]
    public MentorWorkplaceRequest?[] Workplaces { get; init; } = null!;

    public MentorWorkplaceInput[] ToChoices() => Workplaces
        .Select((workplace, index) => workplace?.ToChoice()
            ?? throw new DomainException($"Workplaces[{index}]", "Заполните данные места работы."))
        .ToArray();
}

public sealed class MentorWorkplaceRequest
{
    public Guid? Id { get; init; }

    [Required, MaxLength(MentorWorkplace.TextMaxLength)]
    public string Company { get; init; } = null!;

    [Required, MaxLength(MentorWorkplace.TextMaxLength)]
    public string Title { get; init; } = null!;

    [Required]
    public int? StartYear { get; init; }

    public int? EndYear { get; init; }

    [Required]
    public bool? IsCurrent { get; init; }

    public MentorWorkplaceInput ToChoice() =>
        new(Id, Company, Title, StartYear!.Value, EndYear, IsCurrent!.Value);
}
