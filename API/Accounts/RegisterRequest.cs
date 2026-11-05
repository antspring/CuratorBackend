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

    [Required]
    [MaxLength(100)]
    public string FirstName { get; init; } = null!;

    [MaxLength(100)]
    public string? LastName { get; init; }

    [Required]
    [MaxLength(100)]
    public string Timezone { get; init; } = null!;

    [Required]
    [MinLength(1)]
    [MaxLength(2)]
    public string[] Roles { get; init; } = null!;
}
