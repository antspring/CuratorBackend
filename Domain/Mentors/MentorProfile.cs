using Domain.Common;

namespace Domain.Mentors;

public sealed class MentorProfile
{
    public const int MaxWorkplaces = 20;

    private readonly List<MentorWorkplace> workplaces = [];
    private readonly List<MentorSkill> ownedSkills = [];
    private readonly List<MentorExpectedSkill> skills = [];
    private readonly List<MentorDirection> directions = [];

    private MentorProfile()
    {
    }

    public MentorProfile(Guid userId, MentorAnswers answers)
    {
        UserId = Require.Id(userId, nameof(UserId));
        Update(answers);
    }

    public Guid UserId { get; private set; }

    public Guid PositionId { get; private set; }

    public Guid ExperienceId { get; private set; }

    public Guid FormatId { get; private set; }

    public Guid AvailabilityId { get; private set; }

    public Guid PricingModelId { get; private set; }

    public decimal Price { get; private set; }

    public bool HasFreeTrial { get; private set; }

    public IReadOnlyCollection<MentorSkill> OwnedSkills => ownedSkills;

    public IReadOnlyCollection<MentorExpectedSkill> Skills => skills;

    public IReadOnlyCollection<MentorDirection> Directions => directions;

    public IReadOnlyCollection<MentorWorkplace> Workplaces => workplaces;

    public void ReplaceWorkplaces(IReadOnlyList<MentorWorkplaceInput> choices, int currentYear)
    {
        if (choices.Count > MaxWorkplaces)
        {
            throw new DomainException(nameof(Workplaces), $"Можно указать до {MaxWorkplaces} мест работы.");
        }

        var ids = Require.Ids(choices.Where(choice => choice.Id.HasValue).Select(choice => choice.Id!.Value),
            0, MaxWorkplaces, nameof(Workplaces)).ToHashSet();
        var existing = workplaces.ToDictionary(workplace => workplace.Id);
        var validated = choices.Select((choice, index) =>
        {
            var field = $"Workplaces[{index}]";
            if (choice.Id is { } id && !existing.ContainsKey(id))
            {
                throw new DomainException($"{field}.Id", "Место работы не найдено в вашем профиле.");
            }

            return MentorWorkplace.Validate(choice, currentYear, field);
        }).ToArray();

        var unmatched = workplaces.Where(workplace => !ids.Contains(workplace.Id))
            .OrderBy(workplace => workplace.SortOrder)
            .ToList();

        for (var index = 0; index < validated.Length; index++)
        {
            var choice = validated[index];
            var workplace = choice.Id is { } id ? existing[id] : unmatched.Find(candidate => candidate.Matches(choice));
            if (workplace is null)
            {
                workplaces.Add(new MentorWorkplace(UserId, choice, index));
                continue;
            }

            workplace.Update(choice, index);
            unmatched.Remove(workplace);
        }

        workplaces.RemoveAll(unmatched.Contains);
    }

    public void Update(MentorAnswers answers)
    {
        ArgumentNullException.ThrowIfNull(answers);

        PositionId = answers.PositionId;
        ExperienceId = answers.ExperienceId;
        FormatId = answers.FormatId;
        AvailabilityId = answers.AvailabilityId;
        PricingModelId = answers.PricingModelId;
        Price = answers.Price;
        HasFreeTrial = answers.HasFreeTrial;

        SyncOwnedSkills(answers.OwnedSkills);
        SyncDirections(answers.DirectionIds);
    }

    public void ReplaceSkills(IReadOnlyList<MentorSkillChoice> choices)
    {
        MentorAnswers.ValidateSkills(choices, 0, nameof(Skills));
        var levels = choices.ToDictionary(choice => choice.SkillId, choice => choice.SkillLevelId);

        skills.RemoveAll(skill => !levels.ContainsKey(skill.SkillId));

        foreach (var skill in skills)
        {
            skill.SkillLevelId = levels[skill.SkillId];
            levels.Remove(skill.SkillId);
        }

        skills.AddRange(levels.Select(level => new MentorExpectedSkill(UserId, level.Key, level.Value)));
    }

    private void SyncOwnedSkills(IReadOnlyList<MentorSkillChoice> choices)
    {
        var levels = choices.ToDictionary(choice => choice.SkillId, choice => choice.SkillLevelId);

        ownedSkills.RemoveAll(skill => !levels.ContainsKey(skill.SkillId));

        foreach (var skill in ownedSkills)
        {
            skill.SkillLevelId = levels[skill.SkillId];
        }

        var existing = ownedSkills.Select(skill => skill.SkillId).ToHashSet();
        ownedSkills.AddRange(choices
            .Where(choice => !existing.Contains(choice.SkillId))
            .Select(choice => new MentorSkill(UserId, choice.SkillId, choice.SkillLevelId)));
    }

    private void SyncDirections(IReadOnlyList<Guid> directionIds)
    {
        directions.RemoveAll(direction => !directionIds.Contains(direction.DirectionId));

        var existing = directions.Select(direction => direction.DirectionId).ToHashSet();
        directions.AddRange(directionIds
            .Where(id => !existing.Contains(id))
            .Select(id => new MentorDirection(UserId, id)));
    }
}
