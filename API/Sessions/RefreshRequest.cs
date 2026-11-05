using System.ComponentModel.DataAnnotations;

namespace API.Sessions;

public sealed class RefreshRequest
{
    [Required]
    [StringLength(512, MinimumLength = 1)]
    public string RefreshToken { get; init; } = null!;
}
