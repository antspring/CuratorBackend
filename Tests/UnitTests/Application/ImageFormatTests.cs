using Application.Files;

namespace UnitTests.Application;

public class ImageFormatTests
{
    [Fact]
    public void Detect_Png()
    {
        Assert.Equal(ImageFormat.Png, ImageFormat.Detect([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00]));
    }

    [Fact]
    public void Detect_Jpeg()
    {
        Assert.Equal(ImageFormat.Jpeg, ImageFormat.Detect([0xFF, 0xD8, 0xFF, 0xE0, 0x00]));
    }

    [Theory]
    [InlineData(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D })]
    [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 })]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E })]
    [InlineData(new byte[] { 0xFF, 0xD8 })]
    [InlineData(new byte[0])]
    public void Detect_ReturnsNullForOtherOrTruncatedContent(byte[] header)
    {
        Assert.Null(ImageFormat.Detect(header));
    }
}
