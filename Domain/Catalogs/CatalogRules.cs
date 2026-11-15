using System.Text.RegularExpressions;
using Domain.Common;

namespace Domain.Catalogs;

public static partial class CatalogRules
{
    public const int CodeMaxLength = 100;
    public const int NameMaxLength = 150;

    public static string RequireCode(string? code)
    {
        if (code is null || code.Length > CodeMaxLength || !CodePattern().IsMatch(code))
        {
            throw new DomainException("Code", "Код — латиница в нижнем регистре, цифры и дефисы, не длиннее 100 символов.");
        }

        return code;
    }

    public static string RequireName(string? name)
    {
        var trimmed = name?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            throw new DomainException("Name", "Название не может быть пустым.");
        }

        return trimmed.Length <= NameMaxLength
            ? trimmed
            : throw new DomainException("Name", $"Не длиннее {NameMaxLength} символов.");
    }

    public static string Normalize(string value) => value.Trim().ToUpperInvariant();

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex CodePattern();
}
