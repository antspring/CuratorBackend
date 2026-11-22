namespace Application.Mentors;

public sealed record LookupView(Guid Id, string Name);

public sealed record MentorSkillView(LookupView Skill, LookupView Level);

public sealed record MentorCardView(
    Guid UserId,
    string FirstName,
    string LastName,
    int Age,
    Uri? PhotoUrl,
    LookupView City,
    LookupView Language,
    LookupView Position,
    LookupView Experience,
    IReadOnlyList<MentorSkillView> Skills,
    IReadOnlyList<LookupView> Directions,
    LookupView Format,
    LookupView Availability,
    LookupView PricingModel,
    decimal Price,
    bool HasFreeTrial,
    decimal? Rating,
    int ReviewCount,
    int CompletedMentorshipCount,
    DateTimeOffset? NextAvailableAt);
