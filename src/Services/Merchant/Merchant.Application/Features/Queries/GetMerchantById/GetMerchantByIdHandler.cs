namespace Merchant.Application.Features.Queries.GetMerchantById;

public class GetMerchantByIdHandler
{
    private readonly IMerchantRepository _repository;

    public GetMerchantByIdHandler(IMerchantRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<MerchantDto>> Handle(GetMerchantByIdQuery query, CancellationToken cancellationToken = default)
    {
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