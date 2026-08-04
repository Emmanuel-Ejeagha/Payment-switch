using BuildingBlocks.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;
using Payment.Domain;

namespace Payment.Application.Features.Command.DeleteCustomer;

public class DeleteCustomerHandler
{
    private readonly ICustomerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<DeleteCustomerCommand> _validator;
    private readonly ILogger<DeleteCustomerHandler> _logger;

    public DeleteCustomerHandler(
        ICustomerRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<DeleteCustomerCommand> validator,
        ILogger<DeleteCustomerHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteCustomerCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Customer {CustomerId}", nameof(DeleteCustomerCommand), command.Id);

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var customer = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (customer is null || customer.Deleted || customer.MerchantId != command.MerchantId)
            return PaymentErrors.CustomerNotFound(command.Id);

        customer.MarkDeleted();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted customer {Code}", customer.Code);

        return Result.Success();
    }
}
