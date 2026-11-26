using API.Profiles;
using Application.Mentors;

namespace API.Mentors;

public sealed record LookupResponse(Guid Id, string Name)
{
    public static LookupResponse From(LookupView lookup) => new(lookup.Id, lookup.Name);
}

public sealed record MentorSkillViewResponse(LookupResponse Skill, LookupResponse Level);

public sealed record MentorResponse(
    Guid UserId,
    string FirstName,
    string LastName,
    int Age,
    Uri? PhotoUrl,
    LookupResponse City,
    LookupResponse Language,
    LookupResponse Position,
    MentorCurrentWorkplaceView? CurrentWorkplace,
    LookupResponse Experience,
    IReadOnlyList<MentorSkillViewResponse> Skills,
    IReadOnlyList<LookupResponse> Directions,
    LookupResponse Format,
    LookupResponse Availability,
    LookupResponse PricingModel,
    string Price,
    bool HasFreeTrial,
    decimal? Rating,
    int ReviewCount,
    int CompletedMentorshipCount,
    DateTimeOffset? NextAvailableAt)
{
    public static MentorResponse From(MentorCardView card) =>
        new(
            card.UserId,
            card.FirstName,
            card.LastName,
            card.Age,
            card.PhotoUrl,
            LookupResponse.From(card.City),
            LookupResponse.From(card.Language),
            LookupResponse.From(card.Position),
            card.CurrentWorkplace,
            LookupResponse.From(card.Experience),
            card.Skills.Select(skill => new MentorSkillViewResponse(LookupResponse.From(skill.Skill), LookupResponse.From(skill.Level))).ToList(),
            card.Directions.Select(LookupResponse.From).ToList(),
            LookupResponse.From(card.Format),
            LookupResponse.From(card.Availability),
            LookupResponse.From(card.PricingModel),
            Money.Format(card.Price)!,
            card.HasFreeTrial,
            card.Rating,
            card.ReviewCount,
            card.CompletedMentorshipCount,
            card.NextAvailableAt);
}
