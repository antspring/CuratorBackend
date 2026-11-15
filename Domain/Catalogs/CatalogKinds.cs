namespace Domain.Catalogs;

public static class CatalogKinds
{
    public const string Skills = "skills";
    public const string Cities = "cities";
    public const string Languages = "languages";
    public const string SkillLevels = "skill-levels";
    public const string MenteeTasks = "mentee-tasks";
    public const string Formats = "formats";
    public const string Availability = "availability";
    public const string GoalDeadlines = "goal-deadlines";
    public const string Positions = "positions";
    public const string Experience = "experience";
    public const string MentoringDirections = "mentoring-directions";
    public const string PricingModels = "pricing-models";

    public const int MaxLength = 50;

    public static IReadOnlyList<string> ItemKinds { get; } =
    [
        Languages, SkillLevels, MenteeTasks, Formats, Availability,
        GoalDeadlines, Positions, Experience, MentoringDirections, PricingModels,
    ];

    public static IReadOnlyList<string> All { get; } = [Skills, Cities, .. ItemKinds];
}
