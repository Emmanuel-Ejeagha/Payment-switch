using Merchant.Application.Auth;

namespace Merchant.Application.Features.Queries.GetMerchantById;

public record GetMerchantByIdQuery(Guid MerchantId, CallerContext Caller);
