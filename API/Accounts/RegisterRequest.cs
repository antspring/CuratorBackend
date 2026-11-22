using System.ComponentModel.DataAnnotations;

namespace API.Accounts;

public sealed class RegisterRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } = null!;

    [Required]
    [StringLength(128, MinimumLength = 12)]
    public string Password { get; init; } = null!;
}
