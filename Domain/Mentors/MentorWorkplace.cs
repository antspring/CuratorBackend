using Domain.Common;

namespace Domain.Mentors;

public sealed record MentorWorkplaceInput(
    Guid? Id, string Company, string Title, int StartYear, int? EndYear, bool IsCurrent);

public sealed class MentorWorkplace
{
    public const int TextMaxLength = 200;

    private MentorWorkplace()
    {
    }

    internal MentorWorkplace(Guid mentorId, MentorWorkplaceInput choice, int sortOrder)
    {
        MentorId = mentorId;
        Id = Guid.CreateVersion7();
        Update(choice, sortOrder);
    }

    public Guid MentorId { get; private set; }

    public Guid Id { get; private set; }

    public string Company { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    public int StartYear { get; private set; }

    public int? EndYear { get; private set; }

    public bool IsCurrent { get; private set; }

    public int SortOrder { get; private set; }

    internal static MentorWorkplaceInput Validate(MentorWorkplaceInput choice, int currentYear, string field)
    {
        if (choice.StartYear < 1 || choice.StartYear > currentYear)
        {
            throw new DomainException($"{field}.StartYear", "Год начала должен быть положительным и не позже текущего года.");
        }

        if (choice.EndYear is { } endYear && (choice.IsCurrent || endYear < choice.StartYear || endYear > currentYear))
        {
            throw new DomainException($"{field}.EndYear", choice.IsCurrent
                ? "У текущего места работы нет года окончания."
                : "Год окончания должен быть между годом начала и текущим годом.");
        }

        return choice with
        {
            Company = Require.Text(choice.Company, 1, TextMaxLength, $"{field}.Company"),
            Title = Require.Text(choice.Title, 1, TextMaxLength, $"{field}.Title"),
        };
    }

    internal bool Matches(MentorWorkplaceInput choice) =>
        (Company, Title, StartYear, EndYear, IsCurrent) ==
        (choice.Company, choice.Title, choice.StartYear, choice.EndYear, choice.IsCurrent);

    internal void Update(MentorWorkplaceInput choice, int sortOrder)
    {
        Company = choice.Company;
        Title = choice.Title;
        StartYear = choice.StartYear;
        EndYear = choice.EndYear;
        IsCurrent = choice.IsCurrent;
        SortOrder = sortOrder;
    }
}
