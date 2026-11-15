namespace Domain.Files;

public static class FilePurposes
{
    public const string Avatar = "avatar";

    public const int MaxLength = 30;

    public static IReadOnlyList<string> All { get; } = [Avatar];
}
