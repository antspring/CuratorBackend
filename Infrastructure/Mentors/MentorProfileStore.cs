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
        var mentor = await db.MentorProfiles.AsNoTracking()
            .Include(profile => profile.Skills)
            .Include(profile => profile.Directions)
            .AsSplitQuery()
            .SingleOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);

        if (mentor is null)
        {
            return null;
        }

        var personal = await personalProfiles.LoadAsync(userId, cancellationToken);

        return new MentorProfileSnapshot(personal.Profile, mentor, personal.PhotoStorageKey);
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
            .Include(existing => existing.Skills)
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
}
