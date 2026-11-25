using System.ComponentModel.DataAnnotations;

namespace API.Accounts;

public sealed class ConfirmRegistrationRequest
{
    [Required]
    [RegularExpression("^[0-9]{6}$")]
    public string Code { get; init; } = null!;
}
