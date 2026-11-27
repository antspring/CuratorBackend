using System.Transactions;
using Application.Mentees;
using Application.Profiles;
using Domain.Files;
using Domain.Mentees;
using Domain.Users;
using Infrastructure.Persistence;
using Infrastructure.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Mentees;

internal sealed class MenteeProfileStore(AppDbContext db, PersonalProfileStore personalProfiles) : IMenteeProfileStore
{
    public async Task<MenteeProfileSnapshot?> FindAsync(Guid userId, CancellationToken cancellationToken)
    {
        var mentee = await WithDetails(db.MenteeProfiles.AsNoTracking())
            .SingleOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);

        if (mentee is null)
        {
            return null;
        }

        var personal = await personalProfiles.LoadAsync(userId, cancellationToken);

        return new MenteeProfileSnapshot(personal.Profile, mentee, personal.PhotoStorageKey);
    }

    public async Task<MenteeProfileSnapshot?> PatchAsync(
        Guid userId, MenteeProfilePatch patch, StoredFile? photo, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = Transaction.Current is null
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        if (!await personalProfiles.LockUserAsync(userId, cancellationToken))
        {
            return null;
        }

        var mentee = await WithDetails(db.MenteeProfiles)
            .SingleOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);
        if (mentee is null)
        {
            return null;
        }

        var personal = await db.UserProfiles.SingleAsync(profile => profile.UserId == userId, cancellationToken);
        if (patch.Personal is { } input)
        {
            personal.Update(new PersonalData(input.FirstName, input.LastName, input.Age, input.CityId, personal.LanguageId, photo), now);
            mentee.UpdatePersonalDetails(input.Status, input.University, input.Specialty, input.AboutMe);
        }

        if (patch.OwnedSkills is { } skills)
        {
            mentee.ReplaceOwnedSkills(skills);
        }

        if (patch.Goals is { } goals)
        {
            mentee.ReplaceGoals(goals);
        }

        if (patch.Availability is { } availability)
        {
            mentee.UpdateAvailability(availability);
        }

        personal.MarkUpdated(now);
        await db.SaveChangesAsync(cancellationToken);
        var photoStorageKey = await personalProfiles.FindPhotoStorageKeyAsync(personal.PhotoFileId, cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return new MenteeProfileSnapshot(personal, mentee, photoStorageKey);
    }

    public async Task<SaveProfileOutcome> SaveAsync(
        Guid userId,
        PersonalData personal,
        MenteeAnswers answers,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = Transaction.Current is null
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var rejection = await personalProfiles.StageAsync(userId, Roles.Mentee, Roles.Mentor, personal, now, cancellationToken);

        if (rejection is { } outcome)
        {
            return outcome;
        }

        var mentee = await db.MenteeProfiles
            .Include(existing => existing.Skills)
            .Include(existing => existing.OwnedSkills)
            .Include(existing => existing.Tasks)
            .AsSplitQuery()
            .SingleOrDefaultAsync(existing => existing.UserId == userId, cancellationToken);

        if (mentee is null)
        {
            db.MenteeProfiles.Add(new MenteeProfile(userId, answers));
        }
        else
        {
            mentee.Update(answers);
        }

        await db.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return SaveProfileOutcome.Saved;
    }

    private static IQueryable<MenteeProfile> WithDetails(IQueryable<MenteeProfile> profiles) =>
        profiles.Include(profile => profile.Skills)
            .Include(profile => profile.OwnedSkills)
            .Include(profile => profile.Goals).ThenInclude(goal => goal.Directions)
            .Include(profile => profile.Tasks)
            .AsSplitQuery();
}
