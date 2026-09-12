using BuildingBlocks.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Extensions;

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
            "Identity.EmailAlreadyInUse" => 409,
            "Identity.InvalidCredentials" => 401,
            "Identity.AccountLocked" => 401,
            "Identity.UserNotFound" => 404,
            "Identity.NotAuthorized" => 403,
            "Identity.UserInactive" => 403,
            "Identity.EmailNotVerified" => 403,
            "Identity.EmailAlreadyVerified" => 409,
            "Identity.InvalidVerificationToken" => 400,
            "Identity.VerificationTokenExpired" => 400,
            "Identity.InvalidCurrentPassword" => 400,
            "Identity.InvalidPasswordResetToken" => 400,
            "Identity.PasswordResetTokenExpired" => 400,
            "Identity.ConcurrencyConflict" => 409,
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