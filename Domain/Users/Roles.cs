namespace Domain.Users;

public static class Roles
{
    public const string Mentee = "mentee";
    public const string Mentor = "mentor";
    public const string Curator = "curator";
    public const string Administrator = "administrator";

    public static IReadOnlyList<string> All { get; } = [Mentee, Mentor, Curator, Administrator];

    public static IReadOnlyList<string> SelfAssignable { get; } = [Mentee, Mentor];
}
