using BuildingBlocks.Shared.Results;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;

namespace Payment.Application.Features.Queries.ListPaymentIntentsByMerchant;

public class ListPaymentIntentsByMerchantHandler
{
    private readonly IPaymentIntentRepository _repository;

    public ListPaymentIntentsByMerchantHandler(IPaymentIntentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<PaymentIntentDto>>> Handle(ListPaymentIntentsByMerchantQuery query, CancellationToken cancellationToken = default)
    {
        var intents = await _repository.ListByMerchantAsync(query.MerchantId, query.Skip, query.Take, cancellationToken);
        return intents;
    }
}
