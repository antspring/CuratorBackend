namespace API.Errors;

public static class ErrorCodes
{
    public const string ValidationFailed = "validation_failed";
    public const string InvalidCursor = "invalid_cursor";
    public const string Unauthenticated = "unauthenticated";
    public const string InvalidVerificationCode = "invalid_verification_code";
    public const string VerificationUnavailable = "verification_unavailable";
    public const string InvalidCredentials = "invalid_credentials";
    public const string InvalidRefreshToken = "invalid_refresh_token";
    public const string Forbidden = "forbidden";
    public const string ResourceNotFound = "resource_not_found";
    public const string AccountExists = "account_exists";
    public const string RoleConflict = "role_conflict";
    public const string FileTooLarge = "file_too_large";
    public const string UnsupportedMediaType = "unsupported_media_type";
    public const string RateLimitExceeded = "rate_limit_exceeded";
    public const string InternalError = "internal_error";

    public static string FromStatusCode(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => ValidationFailed,
        StatusCodes.Status401Unauthorized => Unauthenticated,
        StatusCodes.Status403Forbidden => Forbidden,
        StatusCodes.Status404NotFound => ResourceNotFound,
        StatusCodes.Status413PayloadTooLarge => FileTooLarge,
        StatusCodes.Status415UnsupportedMediaType => UnsupportedMediaType,
        StatusCodes.Status429TooManyRequests => RateLimitExceeded,
        _ => InternalError,
    };
}
