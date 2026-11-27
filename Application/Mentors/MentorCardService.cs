using Application.Catalogs;
using Application.Files;

namespace Application.Mentors;

public sealed class MentorCardService(IMentorProfileStore store, ICatalogLookup catalogs, IFileStorage storage)
{
    public async Task<MentorCardView?> FindAsync(Guid mentorId, CancellationToken cancellationToken)
    {
        if (await store.FindAsync(mentorId, cancellationToken) is not { } snapshot)
        {
            return null;
        }

        var personal = snapshot.Personal;
        var mentor = snapshot.Mentor;

        Guid[] ids =
        [
            personal.CityId,
            personal.LanguageId,
            mentor.PositionId,
            mentor.ExperienceId,
            mentor.FormatId,
            mentor.AvailabilityId,
            mentor.PricingModelId,
            .. mentor.OwnedSkills.SelectMany(skill => new[] { skill.SkillId, skill.SkillLevelId }),
            .. mentor.Directions.Select(direction => direction.DirectionId),
        ];

        var found = await catalogs.FindAsync(ids.Distinct().ToList(), cancellationToken);
        LookupView Lookup(Guid id) => new(id, found[id].Name);

        var currentWorkplace = mentor.Workplaces.Where(workplace => workplace.IsCurrent)
            .MinBy(workplace => workplace.SortOrder);

        return new MentorCardView(
            mentor.UserId,
            personal.FirstName,
            personal.LastName,
            personal.Age,
            snapshot.PhotoStorageKey is null ? null : storage.GetPublicUrl(snapshot.PhotoStorageKey),
            Lookup(personal.CityId),
            Lookup(personal.LanguageId),
            Lookup(mentor.PositionId),
            currentWorkplace is null ? null : new MentorCurrentWorkplaceView(currentWorkplace.Company, currentWorkplace.Title),
            Lookup(mentor.ExperienceId),
            mentor.OwnedSkills
                .Select(skill => new MentorSkillView(Lookup(skill.SkillId), Lookup(skill.SkillLevelId)))
                .OrderBy(skill => skill.Skill.Name, StringComparer.Ordinal)
                .ToList(),
            mentor.Directions
                .Select(direction => found[direction.DirectionId])
                .OrderBy(direction => direction.SortOrder)
                .Select(direction => new LookupView(direction.Id, direction.Name))
                .ToList(),
            Lookup(mentor.FormatId),
            Lookup(mentor.AvailabilityId),
            Lookup(mentor.PricingModelId),
            mentor.Price,
            mentor.HasFreeTrial,
            Rating: null,
            ReviewCount: 0,
            CompletedMentorshipCount: 0,
            NextAvailableAt: null);
    }
}
