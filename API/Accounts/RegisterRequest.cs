using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace API.Accounts;

public sealed class RegisterRequest : IValidatableObject
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } = null!;

    [Required]
    [StringLength(128, MinimumLength = 12)]
    public string Password { get; init; } = null!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!MailAddress.TryCreate(Email, out var mailbox) || mailbox.Address != Email.Trim())
        {
            yield return new ValidationResult("Укажите один адрес электронной почты.", [nameof(Email)]);
        }
    }
}
