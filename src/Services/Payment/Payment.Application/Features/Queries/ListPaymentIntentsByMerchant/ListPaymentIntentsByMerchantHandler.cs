using BuildingBlocks.Shared.Paging;
using BuildingBlocks.Shared.Results;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Payment.Application.Features.Queries.ListPaymentIntentsByMerchant;

public class ListPaymentIntentsByMerchantHandler
{
    private readonly IPaymentIntentRepository _repository;
    private readonly ILogger<ListPaymentIntentsByMerchantHandler> _logger;

    public ListPaymentIntentsByMerchantHandler(IPaymentIntentRepository repository, ILogger<ListPaymentIntentsByMerchantHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<PagedData<PaymentIntentDto>>> Handle(ListPaymentIntentsByMerchantQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId}", nameof(ListPaymentIntentsByMerchantQuery), query.MerchantId);
        var intents = await _repository.ListByMerchantAsync(query.MerchantId, query.Skip, query.Take, cancellationToken);
        var total = await _repository.CountByMerchantAsync(query.MerchantId, cancellationToken);
        return new PagedData<PaymentIntentDto>(intents, total);
    }
}
