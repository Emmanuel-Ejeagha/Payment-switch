using Merchant.Application.Auth;

namespace Merchant.Application.Features.Commands.UpdateContactDetails;

public record UpdateContactDetailsCommand(
    Guid MerchantId,
    string? Phone,
    string? Address,
    string? ContactPerson,
    CallerContext Caller
);