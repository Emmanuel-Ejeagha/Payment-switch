using Microsoft.Extensions.Logging;

namespace Merchant.Application.Features.Commands.UpdateSettlementInfo;

public class UpdateSettlementInfoHandler
{
    private readonly IMerchantRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateSettlementInfoCommand> _validator;
    private readonly ILogger<UpdateSettlementInfoHandler> _logger;

    public UpdateSettlementInfoHandler(
        IMerchantRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<UpdateSettlementInfoCommand> validator,
        ILogger<UpdateSettlementInfoHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateSettlementInfoCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId}", nameof(UpdateSettlementInfoCommand), command.MerchantId);

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var merchant = await _repository.GetByIdAsync(command.MerchantId, cancellationToken);
        if (merchant == null)
            return MerchantErrors.MerchantNotFound(command.MerchantId);

        if (!command.Caller.CanAccess(merchant.OwnerId))
            return MerchantErrors.Unauthorized();

        var settlementInfo = new SettlementInfo(
            command.BankAccountName,
            command.BankAccountNumber,
            command.BankName,
            command.SettlementCurrency,
            command.SettlementSchedule);

        merchant.UpdateSettlementInfo(settlementInfo);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}