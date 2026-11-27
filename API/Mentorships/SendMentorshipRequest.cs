using System.ComponentModel.DataAnnotations;
using Domain.Mentorships;

namespace API.Mentorships;

public sealed class SendMentorshipRequest
{
    [Required]
    public Guid? MentorId { get; init; }

    [Required]
    public int? DurationMonths { get; init; }

    [MaxLength(MentorshipRequest.MessageMaxLength)]
    public string? Message { get; init; }
}
