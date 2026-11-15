using Domain.Common;
using Domain.Files;

namespace UnitTests.Domain;

public class StoredFileTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 20, 0, 0, TimeSpan.FromHours(5));

    [Theory]
    [InlineData("photo.png", "photo.png")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData(@"C:\Users\anna\photo.jpg", "photo.jpg")]
    [InlineData("  my\tphoto\u0000.png  ", "myphoto.png")]
    [InlineData("", StoredFile.FallbackName)]
    [InlineData("dir/", StoredFile.FallbackName)]
    [InlineData("..", StoredFile.FallbackName)]
    [InlineData(null, StoredFile.FallbackName)]
    public void SanitizeName_KeepsOnlyPrintableLastSegment(string? name, string expected)
    {
        Assert.Equal(expected, StoredFile.SanitizeName(name));
    }

    [Fact]
    public void SanitizeName_TruncatesLongName()
    {
        var name = new string('a', StoredFile.NameMaxLength + 10);

        Assert.Equal(StoredFile.NameMaxLength, StoredFile.SanitizeName(name).Length);
    }

    [Fact]
    public void Constructor_StoresUtcTimestamp()
    {
        var file = Create();

        Assert.Equal(TimeSpan.Zero, file.CreatedAt.Offset);
        Assert.Equal(Now.UtcDateTime, file.CreatedAt.UtcDateTime);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(StoredFile.MaxSizeBytes + 1)]
    public void Constructor_RejectsSizeOutOfRange(long size)
    {
        var exception = Assert.Throws<DomainException>(() => Create(size: size));

        Assert.Equal(nameof(StoredFile.SizeBytes), exception.Field);
    }

    [Fact]
    public void Constructor_RejectsUnknownPurpose()
    {
        var exception = Assert.Throws<DomainException>(() => Create(purpose: "message_attachment"));

        Assert.Equal(nameof(StoredFile.Purpose), exception.Field);
    }

    private static StoredFile Create(long size = 100, string purpose = FilePurposes.Avatar) =>
        new(Guid.CreateVersion7(), OwnerId, purpose, "photo.png", "image/png", size, "avatars/key.png", Now);
}
