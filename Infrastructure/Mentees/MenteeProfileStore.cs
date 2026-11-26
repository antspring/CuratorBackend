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
        var mentee = await db.MenteeProfiles.AsNoTracking()
            .Include(profile => profile.Skills)
            .Include(profile => profile.OwnedSkills)
            .Include(profile => profile.Goals).ThenInclude(goal => goal.Directions)
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

    public Task<bool> UpdatePersonalAsync(
        SaveMenteePersonalCommand command, StoredFile? photo, DateTimeOffset now, CancellationToken cancellationToken) =>
        UpdateSectionAsync(command.UserId, db.MenteeProfiles, (mentee, personal) =>
        {
            personal.Update(new PersonalData(command.FirstName, command.LastName, command.Age, command.CityId, personal.LanguageId, photo), now);
            mentee.UpdatePersonalDetails(command.Status, command.University, command.Specialty, command.AboutMe);
        }, cancellationToken);

    public Task<bool> ReplaceOwnedSkillsAsync(
        Guid userId, IReadOnlyList<MenteeOwnedSkillChoice> choices, DateTimeOffset now, CancellationToken cancellationToken) =>
        UpdateSectionAsync(userId, db.MenteeProfiles.Include(profile => profile.OwnedSkills),
            (mentee, personal) =>
            {
                mentee.ReplaceOwnedSkills(choices);
                personal.MarkUpdated(now);
            }, cancellationToken);

    public Task<bool> ReplaceGoalsAsync(
        Guid userId, IReadOnlyList<MenteeGoalChoice> choices, DateTimeOffset now, CancellationToken cancellationToken) =>
        UpdateSectionAsync(userId, db.MenteeProfiles.Include(profile => profile.Goals).ThenInclude(goal => goal.Directions).AsSplitQuery(),
            (mentee, personal) =>
            {
                mentee.ReplaceGoals(choices);
                personal.MarkUpdated(now);
            }, cancellationToken);

    private async Task<bool> UpdateSectionAsync(
        Guid userId,
        IQueryable<MenteeProfile> profiles,
        Action<MenteeProfile, UserProfile> update,
        CancellationToken cancellationToken)
    {
        await using var transaction = Transaction.Current is null
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        if (!await personalProfiles.LockUserAsync(userId, cancellationToken))
        {
            return false;
        }

        var mentee = await profiles.SingleOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);
        if (mentee is null)
        {
            return false;
        }

        var personal = await db.UserProfiles.SingleAsync(profile => profile.UserId == userId, cancellationToken);
        update(mentee, personal);
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return true;
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
