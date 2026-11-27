using System.Transactions;
using Application.Mentors;
using Application.Profiles;
using Domain.Mentors;
using Domain.Users;
using Infrastructure.Persistence;
using Infrastructure.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Mentors;

internal sealed class MentorProfileStore(AppDbContext db, PersonalProfileStore personalProfiles) : IMentorProfileStore
{
    public async Task<MentorProfileSnapshot?> FindAsync(Guid userId, CancellationToken cancellationToken)
    {
        var mentor = await WithDetails(db.MentorProfiles.AsNoTracking())
            .SingleOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);

        if (mentor is null)
        {
            return null;
        }

        var personal = await personalProfiles.LoadAsync(userId, cancellationToken);

        return new MentorProfileSnapshot(personal.Profile, mentor, personal.PhotoStorageKey);
    }

    public async Task<MentorProfileSnapshot?> PatchAsync(
        Guid userId, MentorProfilePatch patch, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = Transaction.Current is null
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        if (!await personalProfiles.LockUserAsync(userId, cancellationToken))
        {
            return null;
        }

        var mentor = await WithDetails(db.MentorProfiles)
            .SingleOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);
        if (mentor is null)
        {
            return null;
        }

        if (patch.Workplaces is { } workplaces)
        {
            mentor.ReplaceWorkplaces(workplaces, now.Year);
        }

        if (patch.Skills is { } skills)
        {
            mentor.ReplaceSkills(skills);
        }

        var personal = await db.UserProfiles.SingleAsync(profile => profile.UserId == userId, cancellationToken);
        personal.MarkUpdated(now);
        await db.SaveChangesAsync(cancellationToken);

        var photoStorageKey = await personalProfiles.FindPhotoStorageKeyAsync(personal.PhotoFileId, cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return new MentorProfileSnapshot(personal, mentor, photoStorageKey);
    }

    public async Task<SaveProfileOutcome> SaveAsync(
        Guid userId,
        PersonalData personal,
        MentorAnswers answers,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = Transaction.Current is null
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var rejection = await personalProfiles.StageAsync(userId, Roles.Mentor, Roles.Mentee, personal, now, cancellationToken);

        if (rejection is { } outcome)
        {
            return outcome;
        }

        var mentor = await db.MentorProfiles
            .Include(existing => existing.OwnedSkills)
            .Include(existing => existing.Directions)
            .AsSplitQuery()
            .SingleOrDefaultAsync(existing => existing.UserId == userId, cancellationToken);

        if (mentor is null)
        {
            db.MentorProfiles.Add(new MentorProfile(userId, answers));
        }
        else
        {
            mentor.Update(answers);
        }

        await db.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return SaveProfileOutcome.Saved;
    }

    private static IQueryable<MentorProfile> WithDetails(IQueryable<MentorProfile> profiles) =>
        profiles
            .Include(profile => profile.OwnedSkills)
            .Include(profile => profile.Skills)
            .Include(profile => profile.Directions)
            .Include(profile => profile.Workplaces)
            .AsSplitQuery();
}
