using API.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace API.Files;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RejectOversizedRequestAttribute(long maxBytes) : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        if (context.HttpContext.Request.ContentLength is not { } length || length <= maxBytes)
        {
            return;
        }

        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problem = factory.CreateProblemDetails(
            context.HttpContext,
            statusCode: StatusCodes.Status413PayloadTooLarge,
            detail: "Файл больше 10 MiB.");
        problem.Extensions["code"] = ErrorCodes.FileTooLarge;

        context.Result = new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status413PayloadTooLarge,
            ContentTypes = { "application/problem+json" },
        };
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
