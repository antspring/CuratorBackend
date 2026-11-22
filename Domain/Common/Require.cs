namespace Domain.Common;

public static class Require
{
    public static string Text(string? value, int minLength, int maxLength, string field)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            throw new DomainException(field, "Поле не может быть пустым.");
        }

        if (trimmed.Length < minLength || trimmed.Length > maxLength)
        {
            throw new DomainException(field, $"Длина — от {minLength} до {maxLength} символов.");
        }

        return trimmed;
    }

    public static Guid Id(Guid value, string field) =>
        value != Guid.Empty ? value : throw new DomainException(field, "Значение не выбрано.");

    public static IReadOnlyList<Guid> Ids(IEnumerable<Guid>? values, int minCount, int maxCount, string field)
    {
        var ids = values?.ToList() ?? [];

        if (ids.Count < minCount || ids.Count > maxCount)
        {
            throw new DomainException(field, $"Выберите от {minCount} до {maxCount} значений.");
        }

        if (ids.Contains(Guid.Empty))
        {
            throw new DomainException(field, "Значение не выбрано.");
        }

        if (ids.Distinct().Count() != ids.Count)
        {
            throw new DomainException(field, "Значения не должны повторяться.");
        }

        return ids;
    }
}
