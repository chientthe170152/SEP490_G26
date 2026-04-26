using Microsoft.AspNetCore.Mvc;
using MTCA.Api.Common;
using MTCA.Application.Common.Constants;
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
        var primary = errors.Count > 0 ? errors[0] : Error.Unexpected(ErrorCodes.Unexpected);
        return primary.Type switch
        {
            ErrorType.Validation => controller.StatusCode(StatusCodes.Status422UnprocessableEntity, BuildProblem(ErrorCodes.Validation, 422)),
            ErrorType.Unauthorized => controller.StatusCode(StatusCodes.Status401Unauthorized, BuildProblem(primary.Code, 401)),
            ErrorType.Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden, BuildProblem(primary.Code, 403)),
            ErrorType.NotFound => controller.StatusCode(StatusCodes.Status404NotFound, BuildProblem(primary.Code, 404)),
            ErrorType.Conflict => controller.StatusCode(StatusCodes.Status409Conflict, BuildProblem(primary.Code, 409)),
            ErrorType.Locked => controller.StatusCode(StatusCodes.Status423Locked, BuildProblem(primary.Code, 423)),
            ErrorType.TooManyRequests => controller.StatusCode(StatusCodes.Status429TooManyRequests, BuildProblem(primary.Code, 429)),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError, BuildProblem(primary.Code, 500))
        };
    }

    private static ProblemDetails BuildProblem(string code, int status) => new()
    {
        Type = $"{ProblemTypes.Prefix}{code}",
        Title = code,
        Status = status
    };
}
