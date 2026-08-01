using Merchant.Application.Auth;

namespace Merchant.Application.Features.Queries.GetMerchantByEmail;

public record GetMerchantByEmailQuery(string Email, CallerContext Caller);
