using BuildingBlocks.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace Ledger.API.Extensions;

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
            "Ledger.AccountNotFound" => 404,
            "Ledger.InsufficientFunds" => 400,
            "Ledger.ReserveFailed" => 400,
            "Ledger.CaptureFailed" => 400,
            "Ledger.RefundFailed" => 400,
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