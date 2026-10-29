using MasterNet.Application.Core;
using Microsoft.AspNetCore.Mvc;

namespace MasterNet.WebApi.Extensions;

public static class ResultExtensions
{
    // Maps Result<T> to a consistent IActionResult:
    // - Validation   -> 400 ValidationProblemDetails
    // - NotFound     -> 404 ProblemDetails
    // - Conflict     -> 409 ProblemDetails
    // - Unauthorized -> 401 ProblemDetails
    // - Failure      -> 400 ProblemDetails with message
    // - Success      -> 200 OK with value (or custom success status)
    public static IActionResult FromResult<T>(
        this ControllerBase controller,
        Result<T> result,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return successStatusCode == StatusCodes.Status200OK
                ? controller.Ok(result.Value)
                : controller.StatusCode(successStatusCode, result.Value);
        }

        if (result.Type == ErrorType.Validation && result.ValidationErrors is { Count: > 0 })
        {
            var errorsByField = result.ValidationErrors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            var pd = new ValidationProblemDetails(errorsByField)
            {
                Title = "Validation Errors",
                Status = StatusCodes.Status400BadRequest
            };

            return controller.BadRequest(pd);
        }

        var detail = string.IsNullOrWhiteSpace(result.Error) ? "Request failed." : result.Error;

        var (statusCode, title) = result.Type switch
        {
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not Found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            _ => (StatusCodes.Status400BadRequest, "Bad Request")
        };

        return controller.Problem(
            detail: detail,
            statusCode: statusCode,
            title: title);
    }
}
