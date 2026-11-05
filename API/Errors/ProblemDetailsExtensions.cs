using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace API.Errors;

internal static class ProblemDetailsExtensions
{
    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services.AddControllers(options =>
            {
                options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            })
            .AddJsonOptions(options =>
            {
                options.AllowInputFormatterExceptionMessages = false;
                options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false));
            });

        return services;
    }

    public static IServiceCollection AddApiProblemDetails(this IServiceCollection services)
    {
        services.AddExceptionHandler<DomainExceptionHandler>();

        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            var problem = context.ProblemDetails;
            var statusCode = problem.Status ?? context.HttpContext.Response.StatusCode;

            problem.Extensions.TryAdd("code", ErrorCodes.FromStatusCode(statusCode));
            problem.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

            if (problem is HttpValidationProblemDetails validationProblem)
            {
                NormalizeErrorKeys(validationProblem.Errors);
            }
        });

        return services;
    }

    private static void NormalizeErrorKeys(IDictionary<string, string[]> errors)
    {
        foreach (var key in errors.Keys.Where(key => key.StartsWith("$.", StringComparison.Ordinal)).ToList())
        {
            errors[key[2..]] = errors[key];
            errors.Remove(key);
        }
    }
}
