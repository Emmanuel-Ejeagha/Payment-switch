namespace Merchant.Application.Features.Commands.UpdateMerchantConfig;

public class UpdateMerchantConfigurationHandler
{
    private readonly IMerchantRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IValidator<UpdateMerchantConfigurationCommand> _validator;

    public UpdateMerchantConfigurationHandler(
        IMerchantRepository repository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        IValidator<UpdateMerchantConfigurationCommand> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _validator = validator;
    }

    public async Task<Result> Handle(UpdateMerchantConfigurationCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var merchant = await _repository.GetByIdAsync(command.MerchantId, cancellationToken);
        if (merchant == null)
            return MerchantErrors.MerchantNotFound(command.MerchantId);

        try
        {
            merchant.UpdateConfiguration(command.WebhookUrl, command.PaymentMethods);
        }
        catch (InvalidOperationException)
        {
            return new Error("Merchant.ConfigurationUpdateFailed", "Cannot update configuration for this merchant.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.DispatchAsync(merchant.DomainEvents, cancellationToken);
        return Result.Success();
    }
}