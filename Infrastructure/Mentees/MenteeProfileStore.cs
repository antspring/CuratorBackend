using System.Transactions;
using Application.Mentees;
using Application.Profiles;
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
        var mentee = await db.MenteeProfiles.AsNoTracking()
            .Include(profile => profile.Skills)
            .Include(profile => profile.Tasks)
            .AsSplitQuery()
            .SingleOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);

        if (mentee is null)
        {
            return null;
        }

        var personal = await personalProfiles.LoadAsync(userId, cancellationToken);

        return new MenteeProfileSnapshot(personal.Profile, mentee, personal.PhotoStorageKey);
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
}
