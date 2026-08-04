using BuildingBlocks.Shared.Results;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Application.Mappings;

namespace Payment.Application.Features.Queries.ListInvoices;

public class ListInvoicesHandler
{
    private readonly IInvoiceRepository _repository;

    public ListInvoicesHandler(IInvoiceRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<InvoiceDto>>> Handle(ListInvoicesQuery query, CancellationToken cancellationToken = default)
    {
        var invoices = query.SubscriptionId.HasValue
            ? await _repository.ListBySubscriptionAsync(query.SubscriptionId.Value, query.Skip, query.Take, cancellationToken)
            : await _repository.ListByMerchantAsync(query.MerchantId, query.Skip, query.Take, cancellationToken);

        // A subscription-scoped list must still be limited to the calling merchant.
        var scoped = invoices.Where(i => i.MerchantId == query.MerchantId).Select(i => i.ToDto()).ToList();

        return Result<List<InvoiceDto>>.Success(scoped);
    }
}
