using System.Globalization;

namespace API.Profiles;

internal static class Money
{
    public const string Pattern = @"^\d{1,10}\.\d{2}$";

    public static decimal? Parse(string? value) =>
        value is null ? null : decimal.Parse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

    public static string? Format(decimal? value) =>
        value?.ToString("0.00", CultureInfo.InvariantCulture);
}
