using Domain.Common;
using Domain.Files;
using Domain.Users;

namespace UnitTests.Domain;

public class UserProfileTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CityId = Guid.NewGuid();
    private static readonly Guid LanguageId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 19, 0, 0, TimeSpan.FromHours(5));

    private static PersonalData Personal(
        string firstName = "Анна",
        string lastName = "Иванова",
        int age = 25,
        StoredFile? photo = null) =>
        new(firstName, lastName, age, CityId, LanguageId, photo);

    private static StoredFile Avatar(Guid ownerId) =>
        new(Guid.NewGuid(), ownerId, FilePurposes.Avatar, "me.png", "image/png", 100, $"avatars/{Guid.NewGuid()}.png", Now);

    [Fact]
    public void Constructor_TrimsNamesAndStoresUtcTimestamps()
    {
        var profile = new UserProfile(UserId, Personal("  Анна ", " Иванова "), Now);

        Assert.Equal("Анна", profile.FirstName);
        Assert.Equal("Иванова", profile.LastName);
        Assert.Equal(25, profile.Age);
        Assert.Equal(CityId, profile.CityId);
        Assert.Equal(LanguageId, profile.LanguageId);
        Assert.Null(profile.PhotoFileId);
        Assert.Equal(TimeSpan.Zero, profile.CreatedAt.Offset);
        Assert.Equal(Now.UtcDateTime, profile.UpdatedAt.UtcDateTime);
    }

    [Theory]
    [InlineData("", "Иванова", nameof(PersonalData.FirstName))]
    [InlineData("Анна", "   ", nameof(PersonalData.LastName))]
    public void PersonalData_RejectsEmptyNames(string firstName, string lastName, string field)
    {
        var exception = Assert.Throws<DomainException>(() => Personal(firstName, lastName));

        Assert.Equal(field, exception.Field);
    }

    [Fact]
    public void PersonalData_RejectsTooLongLastName()
    {
        var lastName = new string('а', PersonalData.NameMaxLength + 1);

        var exception = Assert.Throws<DomainException>(() => Personal(lastName: lastName));

        Assert.Equal(nameof(PersonalData.LastName), exception.Field);
    }

    [Theory]
    [InlineData(PersonalData.MinAge - 1)]
    [InlineData(PersonalData.MaxAge + 1)]
    public void PersonalData_RejectsAgeOutOfRange(int age)
    {
        var exception = Assert.Throws<DomainException>(() => Personal(age: age));

        Assert.Equal(nameof(PersonalData.Age), exception.Field);
    }

    [Fact]
    public void PersonalData_RejectsEmptyCatalogIds()
    {
        var exception = Assert.Throws<DomainException>(
            () => new PersonalData("Анна", "Иванова", 25, Guid.Empty, LanguageId, null));

        Assert.Equal(nameof(PersonalData.CityId), exception.Field);
    }

    [Fact]
    public void Constructor_RejectsEmptyUserId()
    {
        Assert.Throws<DomainException>(() => new UserProfile(Guid.Empty, Personal(), Now));
    }

    [Fact]
    public void Update_AcceptsOwnAvatarAndClearsItLater()
    {
        var profile = new UserProfile(UserId, Personal(), Now);
        var avatar = Avatar(UserId);

        profile.Update(Personal(photo: avatar), Now);
        Assert.Equal(avatar.Id, profile.PhotoFileId);

        profile.Update(Personal(), Now);
        Assert.Null(profile.PhotoFileId);
    }

    [Fact]
    public void Update_RejectsSomeoneElsesAvatar()
    {
        var profile = new UserProfile(UserId, Personal(), Now);

        var exception = Assert.Throws<DomainException>(() => profile.Update(Personal(photo: Avatar(Guid.NewGuid())), Now));

        Assert.Equal(nameof(UserProfile.PhotoFileId), exception.Field);
    }
}
