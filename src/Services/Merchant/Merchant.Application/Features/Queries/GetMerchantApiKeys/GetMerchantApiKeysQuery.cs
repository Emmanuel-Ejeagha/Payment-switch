using Merchant.Application.Auth;

namespace Merchant.Application.Features.Queries.GetMerchantApiKeys;

public record GetMerchantApiKeysQuery(Guid MerchantId, CallerContext Caller);
