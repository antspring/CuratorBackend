using Microsoft.AspNetCore.Mvc;

namespace API.Errors;

internal static class ProblemResults
{
    public static ObjectResult ProblemWithCode(this ControllerBase controller, int statusCode, string code, string detail)
    {
        var problem = controller.ProblemDetailsFactory.CreateProblemDetails(
            controller.HttpContext,
            statusCode: statusCode,
            detail: detail);

        problem.Extensions["code"] = code;

        return new ObjectResult(problem)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" },
        };
    }

    public static ActionResult FieldErrors(this ControllerBase controller, string field, IEnumerable<string> messages)
    {
        foreach (var message in messages)
        {
            controller.ModelState.AddModelError(field, message);
        }

        return controller.ValidationProblem(controller.ModelState);
    }
}
