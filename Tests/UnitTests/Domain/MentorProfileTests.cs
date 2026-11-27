using Domain.Common;
using Domain.Mentors;

namespace UnitTests.Domain;

public class MentorProfileTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid SkillA = Guid.NewGuid();
    private static readonly Guid SkillB = Guid.NewGuid();
    private static readonly Guid SkillC = Guid.NewGuid();
    private static readonly Guid Basic = Guid.NewGuid();
    private static readonly Guid Expert = Guid.NewGuid();
    private static readonly Guid Direction = Guid.NewGuid();

    private static MentorAnswers Answers(MentorSkillChoice[]? skills = null, Guid[]? directionIds = null, decimal price = 1500m) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            skills ?? [new(SkillA, Basic)],
            directionIds ?? [Direction],
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            price,
            hasFreeTrial: true);

    [Fact]
    public void Constructor_StoresAnswersAndCollections()
    {
        var profile = new MentorProfile(UserId, Answers([new(SkillA, Basic), new(SkillB, Expert)], price: 0m));

        Assert.Equal(0m, profile.Price);
        Assert.True(profile.HasFreeTrial);
        Assert.Equal([(SkillA, Basic), (SkillB, Expert)], profile.OwnedSkills.Select(skill => (skill.SkillId, skill.SkillLevelId)));
        Assert.All(profile.OwnedSkills, skill => Assert.Equal(UserId, skill.MentorId));
        Assert.Equal([Direction], profile.Directions.Select(direction => direction.DirectionId));
    }

    [Fact]
    public void Update_ChangesLevelInPlaceAndReplacesOtherSkills()
    {
        var profile = new MentorProfile(UserId, Answers([new(SkillA, Basic), new(SkillB, Basic)]));
        var keptSkill = profile.OwnedSkills.Single(skill => skill.SkillId == SkillB);

        profile.Update(Answers([new(SkillB, Expert), new(SkillC, Basic)]));

        Assert.Equal([(SkillB, Expert), (SkillC, Basic)], profile.OwnedSkills.Select(skill => (skill.SkillId, skill.SkillLevelId)));
        Assert.Same(keptSkill, profile.OwnedSkills.First());
    }

    [Fact]
    public void Answers_RejectDuplicateSkillEvenWithDifferentLevels()
    {
        var exception = Assert.Throws<DomainException>(() => Answers([new(SkillA, Basic), new(SkillA, Expert)]));

        Assert.Equal(nameof(MentorAnswers.OwnedSkills), exception.Field);
    }

    [Fact]
    public void Answers_RejectSkillWithoutLevel()
    {
        var exception = Assert.Throws<DomainException>(() => Answers([new(SkillA, Guid.Empty)]));

        Assert.Equal(nameof(MentorAnswers.OwnedSkills), exception.Field);
    }

    [Fact]
    public void Answers_RejectEmptyDirections()
    {
        var exception = Assert.Throws<DomainException>(() => Answers(directionIds: []));

        Assert.Equal(nameof(MentorAnswers.DirectionIds), exception.Field);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(99.999)]
    public void Answers_RejectInvalidPrice(double price)
    {
        var exception = Assert.Throws<DomainException>(() => Answers(price: (decimal)price));

        Assert.Equal(nameof(MentorAnswers.Price), exception.Field);
    }
}
