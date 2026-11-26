using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Registration;

internal sealed class SmtpOptions
{
    [Required]
    public string Host { get; init; } = null!;

    [Range(1, 65535)]
    public int Port { get; init; } = 587;

    public bool StartTls { get; init; } = true;
    public string? Username { get; init; }
    public string? Password { get; init; }

    [Required, EmailAddress]
    public string From { get; init; } = null!;
}
