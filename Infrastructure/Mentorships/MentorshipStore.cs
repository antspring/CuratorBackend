using System.Transactions;
using Application.Mentorships;
using Domain.Mentorships;
using Infrastructure.Persistence;
using Infrastructure.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Mentorships;

internal sealed class MentorshipStore(AppDbContext db, PersonalProfileStore profiles) : IMentorshipStore
{
    public async Task<SendMentorshipRequestResult> SendAsync(MentorshipRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = Transaction.Current is null
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        if (!await profiles.LockUserAsync(request.MenteeId, cancellationToken)
            || !await db.MenteeProfiles.AnyAsync(profile => profile.UserId == request.MenteeId, cancellationToken))
        {
            return new SendMentorshipRequestResult.ProfileIncomplete();
        }

        if (!await db.MentorProfiles.AnyAsync(profile => profile.UserId == request.MentorId, cancellationToken))
        {
            return new SendMentorshipRequestResult.MentorNotFound();
        }

        if (await HasActiveMentorshipAsync(request.MenteeId, request.MentorId, cancellationToken))
        {
            return new SendMentorshipRequestResult.ActiveMentorshipExists();
        }

        if (await db.MentorshipRequests.AnyAsync(existing => existing.MenteeId == request.MenteeId
            && existing.MentorId == request.MentorId && existing.Status == MentorshipRequestStatus.Pending, cancellationToken))
        {
            return new SendMentorshipRequestResult.RequestExists();
        }

        db.MentorshipRequests.Add(request);
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return new SendMentorshipRequestResult.Sent(request);
    }

    public async Task<AcceptMentorshipRequestResult> AcceptAsync(
        Guid requestId, Guid mentorId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = Transaction.Current is null
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var incoming = db.MentorshipRequests.Where(request => request.Id == requestId && request.MentorId == mentorId);
        var menteeId = await incoming.Select(request => (Guid?)request.MenteeId).SingleOrDefaultAsync(cancellationToken);
        if (menteeId is not { } authorId || !await profiles.LockUserAsync(authorId, cancellationToken))
        {
            return new AcceptMentorshipRequestResult.NotFound();
        }

        var request = await incoming.SingleOrDefaultAsync(cancellationToken);
        if (request is null)
        {
            return new AcceptMentorshipRequestResult.NotFound();
        }

        if (request.Status == MentorshipRequestStatus.Accepted)
        {
            var existing = await db.Mentorships.AsNoTracking()
                .SingleAsync(pair => pair.RequestId == requestId, cancellationToken);
            return new AcceptMentorshipRequestResult.Accepted(request, existing);
        }

        if (request.Status != MentorshipRequestStatus.Pending)
        {
            return new AcceptMentorshipRequestResult.InvalidTransition();
        }

        if (await HasActiveMentorshipAsync(request.MenteeId, request.MentorId, cancellationToken))
        {
            return new AcceptMentorshipRequestResult.ActiveMentorshipExists();
        }

        var mentorship = request.Accept(now)!;
        db.Mentorships.Add(mentorship);
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return new AcceptMentorshipRequestResult.Accepted(request, mentorship);
    }

    public Task<MentorshipRequestSnapshot?> FindRequestAsync(Guid requestId, Guid userId, CancellationToken cancellationToken) =>
        WithMentorshipId(db.MentorshipRequests.AsNoTracking().Where(request => request.Id == requestId
            && (request.MenteeId == userId || request.MentorId == userId)))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<MentorshipRequestSnapshot>> ListRequestsAsync(
        Guid userId, MentorshipRequestDirection direction, MentorshipRequestStatus? status,
        MentorshipRequestPosition? after, int limit, CancellationToken cancellationToken)
    {
        var requests = db.MentorshipRequests.AsNoTracking();
        requests = direction == MentorshipRequestDirection.Incoming
            ? requests.Where(request => request.MentorId == userId)
            : requests.Where(request => request.MenteeId == userId);

        if (status is { } requestedStatus)
        {
            requests = requests.Where(request => request.Status == requestedStatus);
        }

        if (after is not null)
        {
            requests = requests.Where(request => request.CreatedAt < after.CreatedAt
                || (request.CreatedAt == after.CreatedAt && request.Id.CompareTo(after.Id) < 0));
        }

        return await WithMentorshipId(requests.OrderByDescending(request => request.CreatedAt)
                .ThenByDescending(request => request.Id).Take(limit))
            .ToListAsync(cancellationToken);
    }

    public Task<Mentorship?> FindMentorshipAsync(Guid mentorshipId, Guid userId, CancellationToken cancellationToken) =>
        db.Mentorships.AsNoTracking().SingleOrDefaultAsync(mentorship => mentorship.Id == mentorshipId
            && (mentorship.MenteeId == userId || mentorship.MentorId == userId), cancellationToken);

    private IQueryable<MentorshipRequestSnapshot> WithMentorshipId(IQueryable<MentorshipRequest> requests) =>
        requests.Select(request => new MentorshipRequestSnapshot(request,
            db.Mentorships.Where(mentorship => mentorship.RequestId == request.Id)
                .Select(mentorship => (Guid?)mentorship.Id).FirstOrDefault()));

    private Task<bool> HasActiveMentorshipAsync(Guid menteeId, Guid mentorId, CancellationToken cancellationToken) =>
        db.Mentorships.AnyAsync(mentorship => mentorship.MenteeId == menteeId && mentorship.MentorId == mentorId
            && mentorship.Status == MentorshipStatus.Active, cancellationToken);
}
