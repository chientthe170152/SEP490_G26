using Microsoft.AspNetCore.Mvc;
using MTCA.Application.Common.Models;

namespace MTCA.Api.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller) =>
        result.IsSuccess
            ? controller.Ok(result.Value)
            : MapFailure(result.Errors, controller);

    public static IActionResult ToActionResult(this Result result, ControllerBase controller) =>
        result.IsSuccess
            ? controller.NoContent()
            : MapFailure(result.Errors, controller);

    private static IActionResult MapFailure(IReadOnlyList<Error> errors, ControllerBase controller)
    {
        var primary = errors.Count > 0 ? errors[0] : Error.Unexpected("UNEXPECTED", "Unexpected error.");
        return primary.Type switch
        {
            ErrorType.Validation => BuildValidationProblem(errors, controller),
            ErrorType.Unauthorized => controller.StatusCode(StatusCodes.Status401Unauthorized, BuildProblem(primary, 401)),
            ErrorType.Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden, BuildProblem(primary, 403)),
            ErrorType.NotFound => controller.StatusCode(StatusCodes.Status404NotFound, BuildProblem(primary, 404)),
            ErrorType.Conflict => controller.StatusCode(StatusCodes.Status409Conflict, BuildProblem(primary, 409)),
            ErrorType.Locked => controller.StatusCode(StatusCodes.Status423Locked, BuildProblem(primary, 423)),
            ErrorType.TooManyRequests => controller.StatusCode(StatusCodes.Status429TooManyRequests, BuildProblem(primary, 429)),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError, BuildProblem(primary, 500))
        };
    }

    private static ProblemDetails BuildProblem(Error error, int status) => new()
    {
        Type = $"https://mtca.local/errors/{error.Code}",
        Title = error.Code,
        Status = status,
        Detail = error.Message
    };

    private static IActionResult BuildValidationProblem(IReadOnlyList<Error> errors, ControllerBase controller)
    {
        var modelState = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
        foreach (var error in errors)
        {
            modelState.AddModelError(error.Code, error.Message);
        }

        var problem = new ValidationProblemDetails(modelState)
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "One or more validation errors occurred.",
            Type = "https://mtca.local/errors/VALIDATION"
        };
        return controller.StatusCode(StatusCodes.Status422UnprocessableEntity, problem);
    }
}
