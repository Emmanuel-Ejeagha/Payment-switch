namespace Merchant.Application.Features.Queries.GetMerchantByEmail;

public class GetMerchantByEmailHandler
{
    private readonly IMerchantRepository _repository;

    public GetMerchantByEmailHandler(IMerchantRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<MerchantDto>> Handle(GetMerchantByEmailQuery query, CancellationToken cancellationToken = default)
    {
        var merchant = await _repository.GetByEmailAsync(query.Email, cancellationToken);
        if (merchant == null)
            return MerchantErrors.MerchantNotFound(Guid.Empty);

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