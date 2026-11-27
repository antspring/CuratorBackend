using Application.Common;
using Domain.Mentorships;

namespace Application.Mentorships;

public sealed class MentorshipService(IMentorshipStore store, TimeProvider timeProvider)
{
    public Task<SendMentorshipRequestResult> SendAsync(
        Guid menteeId, Guid mentorId, int durationMonths, string? message, CancellationToken cancellationToken) =>
        store.SendAsync(new MentorshipRequest(menteeId, mentorId, durationMonths, message, CurrentTime()), cancellationToken);

    public Task<AcceptMentorshipRequestResult> AcceptAsync(Guid requestId, Guid mentorId, CancellationToken cancellationToken) =>
        store.AcceptAsync(requestId, mentorId, CurrentTime(), cancellationToken);

    public Task<MentorshipRequestSnapshot?> FindRequestAsync(Guid requestId, Guid userId, CancellationToken cancellationToken) =>
        store.FindRequestAsync(requestId, userId, cancellationToken);

    public Task<Mentorship?> FindMentorshipAsync(Guid mentorshipId, Guid userId, CancellationToken cancellationToken) =>
        store.FindMentorshipAsync(mentorshipId, userId, cancellationToken);

    public async Task<MentorshipRequestPage?> ListRequestsAsync(
        Guid userId, MentorshipRequestDirection direction, MentorshipRequestStatus? status,
        int limit, string? cursor, CancellationToken cancellationToken)
    {
        MentorshipRequestPosition? after = null;
        if (cursor is not null)
        {
            if (!Cursor.TryDecode<RequestCursor>(cursor, out var decoded) || decoded.UserId != userId
                || decoded.Direction != direction || decoded.Status != status
                || decoded.Position.Id == Guid.Empty || decoded.Position.CreatedAt.Offset != TimeSpan.Zero)
            {
                return null;
            }

            after = decoded.Position;
        }

        var found = await store.ListRequestsAsync(userId, direction, status, after, limit + 1, cancellationToken);
        var items = found.Take(limit).ToArray();
        var nextCursor = found.Count > limit
            ? Cursor.Encode(new RequestCursor(userId, direction, status, new(items[^1].Request.CreatedAt, items[^1].Request.Id)))
            : null;

        return new MentorshipRequestPage(items, nextCursor);
    }

    private DateTimeOffset CurrentTime()
    {
        var now = timeProvider.GetUtcNow();
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond));
    }

    private sealed record RequestCursor(Guid UserId, MentorshipRequestDirection Direction,
        MentorshipRequestStatus? Status, MentorshipRequestPosition Position);
}
