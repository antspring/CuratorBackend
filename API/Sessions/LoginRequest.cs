using System.ComponentModel.DataAnnotations;

namespace API.Sessions;

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } = null!;

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Password { get; init; } = null!;
}
