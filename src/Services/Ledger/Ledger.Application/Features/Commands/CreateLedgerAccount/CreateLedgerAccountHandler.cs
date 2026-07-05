using BuildingBlocks.Shared.Results;
using FluentValidation;
using Ledger.Application.Interfaces;
using Ledger.Domain.Entities;

namespace Ledger.Application.Features.Commands.CreateLedgerAccount;

public class CreateLedgerAccountHandler
{
    private readonly ILedgerAccountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateLedgerAccountCommand> _validator;

    public CreateLedgerAccountHandler(
        ILedgerAccountRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<CreateLedgerAccountCommand> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result> Handle(CreateLedgerAccountCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var existing = await _repository.GetByMerchantIdAsync(command.MerchantId, cancellationToken);
        if (existing is not null)
            return Result.Success(); 

        var account = new LedgerAccount(Guid.NewGuid(), command.MerchantId, command.Currency);
        await _repository.AddAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}