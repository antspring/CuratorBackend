namespace API.Common;

public sealed record PageResponse<T>(IReadOnlyList<T> Items, string? NextCursor);

public static class PageResponse
{
    public const int MinLimit = 1;
    public const int MaxLimit = 100;
    public const int DefaultLimit = 20;
}

public sealed record LookupResponse(Guid Id, string Name);
