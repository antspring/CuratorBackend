using System.Globalization;
using System.Threading.RateLimiting;
using API.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.RateLimiting;

internal static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(AuthRateLimitOptions.SectionName).Get<AuthRateLimitOptions>()
            ?? new AuthRateLimitOptions();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiter.AddPolicy(AuthPolicy, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.PermitLimit,
                    Window = options.Window,
                    QueueLimit = 0,
                }));

            limiter.OnRejected = async (context, cancellationToken) =>
            {
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var delay)
                    ? delay
                    : options.Window;
                var seconds = (int)Math.Ceiling(retryAfter.TotalSeconds);

                context.HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);

                var problemDetailsService = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problemDetailsService.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Type = "https://tools.ietf.org/html/rfc6585#section-4",
                        Title = "Too Many Requests",
                        Detail = $"Слишком много попыток. Повторите через {seconds} с.",
                        Extensions = { ["code"] = ErrorCodes.RateLimitExceeded },
                    },
                });
            };
        });

        return services;
    }

    private sealed class AuthRateLimitOptions
    {
        public const string SectionName = "RateLimiting:Auth";

        public int PermitLimit { get; init; } = 10;

        public TimeSpan Window { get; init; } = TimeSpan.FromMinutes(1);
    }
}
