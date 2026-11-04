using Domain.Common;
using Domain.Users;

namespace UnitTests.Domain;

public class UserProfileTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 19, 0, 0, TimeSpan.FromHours(5));

    [Fact]
    public void Constructor_TrimsNamesAndStoresUtcTimestamps()
    {
        var profile = new UserProfile(UserId, "  Анна ", "  ", "Asia/Yekaterinburg", Now);

        Assert.Equal("Анна", profile.FirstName);
        Assert.Null(profile.LastName);
        Assert.Equal("Asia/Yekaterinburg", profile.TimeZone);
        Assert.Equal(TimeSpan.Zero, profile.CreatedAt.Offset);
        Assert.Equal(Now.UtcDateTime, profile.UpdatedAt.UtcDateTime);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyFirstName(string firstName)
    {
        var exception = Assert.Throws<DomainException>(() => new UserProfile(UserId, firstName, null, "UTC", Now));

        Assert.Equal(nameof(UserProfile.FirstName), exception.Field);
    }

    [Fact]
    public void Constructor_RejectsTooLongLastName()
    {
        var lastName = new string('а', UserProfile.NameMaxLength + 1);

        var exception = Assert.Throws<DomainException>(() => new UserProfile(UserId, "Анна", lastName, "UTC", Now));

        Assert.Equal(nameof(UserProfile.LastName), exception.Field);
    }

    [Theory]
    [InlineData("Mars/Olympus")]
    [InlineData("")]
    public void Constructor_RejectsUnknownTimeZone(string timeZone)
    {
        var exception = Assert.Throws<DomainException>(() => new UserProfile(UserId, "Анна", null, timeZone, Now));

        Assert.Equal(nameof(UserProfile.TimeZone), exception.Field);
    }

    [Fact]
    public void Constructor_RejectsEmptyUserId()
    {
        Assert.Throws<DomainException>(() => new UserProfile(Guid.Empty, "Анна", null, "UTC", Now));
    }
}
