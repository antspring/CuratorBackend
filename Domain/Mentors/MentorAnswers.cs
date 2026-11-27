using Domain.Common;

namespace Domain.Mentors;

public sealed class MentorAnswers
{
    public const int MaxSkills = 50;
    public const int MaxDirections = 20;

    public MentorAnswers(
        Guid positionId,
        Guid experienceId,
        IEnumerable<MentorSkillChoice> ownedSkills,
        IEnumerable<Guid> directionIds,
        Guid formatId,
        Guid availabilityId,
        Guid pricingModelId,
        decimal price,
        bool hasFreeTrial)
    {
        PositionId = Require.Id(positionId, nameof(PositionId));
        ExperienceId = Require.Id(experienceId, nameof(ExperienceId));
        OwnedSkills = ownedSkills?.ToList() ?? [];
        ValidateSkills(OwnedSkills, 1, nameof(OwnedSkills));
        DirectionIds = Require.Ids(directionIds, 1, MaxDirections, nameof(DirectionIds));
        FormatId = Require.Id(formatId, nameof(FormatId));
        AvailabilityId = Require.Id(availabilityId, nameof(AvailabilityId));
        PricingModelId = Require.Id(pricingModelId, nameof(PricingModelId));
        Price = Require.Amount(price, nameof(Price));
        HasFreeTrial = hasFreeTrial;
    }

    public Guid PositionId { get; }

    public Guid ExperienceId { get; }

    public IReadOnlyList<MentorSkillChoice> OwnedSkills { get; }

    public IReadOnlyList<Guid> DirectionIds { get; }

    public Guid FormatId { get; }

    public Guid AvailabilityId { get; }

    public Guid PricingModelId { get; }

    public decimal Price { get; }

    public bool HasFreeTrial { get; }

    internal static void ValidateSkills(IReadOnlyList<MentorSkillChoice> choices, int minCount, string field)
    {
        Require.Ids(choices.Select(choice => choice.SkillId), minCount, MaxSkills, field);

        if (choices.Any(choice => choice.SkillLevelId == Guid.Empty))
        {
            throw new DomainException(field, "Для каждого навыка выберите уровень.");
        }
    }
}
