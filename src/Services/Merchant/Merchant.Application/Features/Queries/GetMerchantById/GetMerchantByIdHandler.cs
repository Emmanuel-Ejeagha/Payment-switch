using Microsoft.Extensions.Logging;

namespace Merchant.Application.Features.Queries.GetMerchantById;

public class GetMerchantByIdHandler
{
    private readonly IMerchantRepository _repository;
    private readonly ILogger<GetMerchantByIdHandler> _logger;

    public GetMerchantByIdHandler(IMerchantRepository repository, ILogger<GetMerchantByIdHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<MerchantDto>> Handle(GetMerchantByIdQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {QueryName} for Merchant {MerchantId}", nameof(GetMerchantByIdQuery), query.MerchantId);

        var merchant = await _repository.GetByIdAsync(query.MerchantId, cancellationToken);
        if (merchant == null)
            return MerchantErrors.MerchantNotFound(query.MerchantId);

        return new MerchantDto(
            merchant.Id,
            merchant.BusinessName.Value,
            merchant.Email.Value,
            merchant.Status.Value,
            merchant.WebhookUrl?.Value,
            merchant.EnabledPaymentMethods.ToList()
        );
    }
}