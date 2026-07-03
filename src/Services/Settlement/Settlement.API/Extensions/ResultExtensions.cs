using BuildingBlocks.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace Settlement.API.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess) return new OkResult();
        return MapErrorsToProblemDetails(result.Errors);
    }

    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess) return new OkObjectResult(result.Value);
        return MapErrorsToProblemDetails(result.Errors);
    }

    private static IActionResult MapErrorsToProblemDetails(IReadOnlyList<Error> errors)
    {
        var first = errors[0];
        var statusCode = first.Code switch
        {
            "Settlement.BatchNotFound" => 404,
            "Settlement.BatchAlreadyCompleted" => 400,
            "Settlement.DuplicateMerchant" => 409,
            _ => 400
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = first.Message,
            Extensions = { ["errors"] = errors.Select(e => new { e.Code, e.Message }) }
        };

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }
}