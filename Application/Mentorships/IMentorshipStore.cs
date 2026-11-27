using Domain.Mentorships;

namespace Application.Mentorships;

public interface IMentorshipStore
{
    Task<SendMentorshipRequestResult> SendAsync(MentorshipRequest request, CancellationToken cancellationToken);

    Task<AcceptMentorshipRequestResult> AcceptAsync(
        Guid requestId, Guid mentorId, DateTimeOffset now, CancellationToken cancellationToken);

    Task<MentorshipRequestSnapshot?> FindRequestAsync(Guid requestId, Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MentorshipRequestSnapshot>> ListRequestsAsync(
        Guid userId, MentorshipRequestDirection direction, MentorshipRequestStatus? status,
        MentorshipRequestPosition? after, int limit, CancellationToken cancellationToken);

    Task<Mentorship?> FindMentorshipAsync(Guid mentorshipId, Guid userId, CancellationToken cancellationToken);
}
