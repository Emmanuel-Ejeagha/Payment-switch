namespace Merchant.Application.Features.Commands.SuspendMerchant;

public class SuspendMerchantHandler
{
    private readonly IMerchantRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IValidator<SuspendMerchantCommand> _validator;

    public SuspendMerchantHandler(
        IMerchantRepository repository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        IValidator<SuspendMerchantCommand> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _validator = validator;
    }

    public async Task<Result> Handle(SuspendMerchantCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var merchant = await _repository.GetByIdAsync(command.MerchantId, cancellationToken);
        if (merchant == null)
            return MerchantErrors.MerchantNotFound(command.MerchantId);

        try
        {
            merchant.Suspend();
        }
        catch (InvalidOperationException)
        {
            return MerchantErrors.InvalidStatusTransition(merchant.Status.Value, "Suspended");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.DispatchAsync(merchant.DomainEvents, cancellationToken);
        return Result.Success();
    }
}