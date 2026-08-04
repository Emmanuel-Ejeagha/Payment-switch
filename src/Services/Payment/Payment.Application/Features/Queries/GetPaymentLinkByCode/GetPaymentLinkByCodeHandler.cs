using BuildingBlocks.Shared.Results;
using Payment.Application.Interfaces;

namespace Payment.Application.Features.Queries.GetPaymentLinkByCode;

public class GetPaymentLinkByCodeHandler
{
    private readonly IPaymentLinkRepository _repository;

    public GetPaymentLinkByCodeHandler(IPaymentLinkRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PaymentLinkDetails>> Handle(GetPaymentLinkByCodeQuery query, CancellationToken cancellationToken = default)
    {
        var link = await _repository.GetByCodeAsync(query.Code, cancellationToken);
        if (link is null)
            return new Error("PaymentLink.NotFound", "Payment link not found.");

        if (!link.Active)
            return new Error("PaymentLink.Inactive", "This payment link has been deactivated.");

        return new PaymentLinkDetails(
            link.Id,
            link.Amount.Amount,
            link.Amount.Currency,
            link.Code,
            link.Description,
            link.Active);
    }
}
