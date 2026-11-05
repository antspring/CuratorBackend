namespace Domain.Common;

public sealed class DomainException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;
}
