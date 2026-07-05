namespace Merchant.Application.Features.Commands.OnboardMerchant;

public class OnboardMerchantHandler
{
    private readonly IMerchantRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IValidator<OnboardMerchantCommand> _validator;

    public OnboardMerchantHandler(
        IMerchantRepository repository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        IValidator<OnboardMerchantCommand> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _validator = validator;
    }

    public async Task<Result<OnboardMerchantResponse>> Handle(OnboardMerchantCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        if (await _repository.ExistsByEmailAsync(command.Email, cancellationToken))
            return MerchantErrors.EmailAlreadyInUse(command.Email);

        var businessName = new BusinessName(command.BusinessName);
        var email = new MerchantEmail(command.Email);
        var merchant = new MerchantEntity(Guid.NewGuid(), businessName, email);

        await _repository.AddAsync(merchant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.DispatchAsync(merchant.DomainEvents, cancellationToken);

        return new OnboardMerchantResponse(merchant.Id);
    }
}