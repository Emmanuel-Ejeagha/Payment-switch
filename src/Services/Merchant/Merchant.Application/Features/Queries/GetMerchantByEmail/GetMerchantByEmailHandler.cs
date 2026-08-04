using Microsoft.Extensions.Logging;

namespace Merchant.Application.Features.Queries.GetMerchantByEmail;

public class GetMerchantByEmailHandler
{
    private readonly IMerchantRepository _repository;
    private readonly ILogger<GetMerchantByEmailHandler> _logger;

    public GetMerchantByEmailHandler(IMerchantRepository repository, ILogger<GetMerchantByEmailHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<MerchantDto>> Handle(GetMerchantByEmailQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {QueryName}", nameof(GetMerchantByEmailQuery));

        var merchant = await _repository.GetByEmailAsync(query.Email, cancellationToken);
        if (merchant == null)
            return MerchantErrors.MerchantNotFound(Guid.Empty);

        if (!query.Caller.CanAccessMerchant(merchant.OwnerId, merchant.Email.Value))
            return MerchantErrors.Unauthorized();

        return new MerchantDto(
            merchant.Id,
            merchant.BusinessName.Value,
            merchant.Email.Value,
            merchant.Status.Value,
            merchant.WebhookUrl?.Value,
            merchant.WebhookSecret?.Value,
            merchant.EnabledPaymentMethods.ToList(),
            merchant.AutoCapture
        );
    }
}