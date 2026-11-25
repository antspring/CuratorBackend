using Application.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Registration;

internal sealed class PendingRegistration
{
    public static readonly TimeSpan RetentionAfterExpiry = TimeSpan.FromHours(24);

    public string NormalizedEmail { get; init; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public Guid VerificationId { get; set; }
    public Guid CodeNonce { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset ResendAvailableAt { get; private set; }
    public DateTimeOffset? DeliveryDueAt { get; private set; }
    public int AttemptsLeft { get; set; }

    public VerificationTicket IssueCode(DateTimeOffset now, VerificationCodes codes)
    {
        CodeNonce = codes.NewNonce(CodeNonce);
        ExpiresAt = now.AddMinutes(10);
        ResendAvailableAt = now.AddSeconds(30);
        DeliveryDueAt = now;
        AttemptsLeft = 5;
        return new VerificationTicket(VerificationId, ExpiresAt, ResendAvailableAt);
    }
}

internal sealed class PendingRegistrationConfiguration : IEntityTypeConfiguration<PendingRegistration>
{
    public void Configure(EntityTypeBuilder<PendingRegistration> builder)
    {
        builder.ToTable("pending_registrations");
        builder.HasKey(row => row.NormalizedEmail);
        builder.HasIndex(row => row.VerificationId).IsUnique();
        builder.HasIndex(row => row.DeliveryDueAt).HasFilter("delivery_due_at IS NOT NULL");
        builder.HasIndex(row => row.ExpiresAt);
    }
}
