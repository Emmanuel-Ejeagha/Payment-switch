using BuildingBlocks.Shared.Results;
using Payment.Application.Interfaces;

namespace Payment.Application.Features.Queries.ListPaymentLinksByMerchant;

public class ListPaymentLinksByMerchantHandler
{
    private readonly IPaymentLinkRepository _repository;

    public ListPaymentLinksByMerchantHandler(IPaymentLinkRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<PaymentLinkDto>>> Handle(ListPaymentLinksByMerchantQuery query, CancellationToken cancellationToken = default)
    {
        var links = await _repository.ListByMerchantAsync(query.MerchantId, query.Skip, query.Take, cancellationToken);

        var dtos = links.Select(l => new PaymentLinkDto(
            l.Id,
            l.Amount.Amount,
            l.Amount.Currency,
            l.Code,
            l.Description,
            l.Active,
            l.CreatedAt
        )).ToList();

        return Result<List<PaymentLinkDto>>.Success(dtos);
    }
}
