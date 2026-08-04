using BuildingBlocks.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Application.Mappings;
using Payment.Domain;

namespace Payment.Application.Features.Command.UpdateCustomer;

public class UpdateCustomerHandler
{
    private readonly ICustomerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateCustomerCommand> _validator;
    private readonly ILogger<UpdateCustomerHandler> _logger;

    public UpdateCustomerHandler(
        ICustomerRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<UpdateCustomerCommand> validator,
        ILogger<UpdateCustomerHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<CustomerDto>> Handle(UpdateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Customer {CustomerId}", nameof(UpdateCustomerCommand), command.Id);

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var customer = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (customer is null || customer.Deleted || customer.MerchantId != command.MerchantId)
            return PaymentErrors.CustomerNotFound(command.Id);

        if (command.Email is not null)
        {
            var email = command.Email.Trim().ToLowerInvariant();
            if (email != customer.Email && await _repository.ExistsByEmailAsync(command.MerchantId, email, cancellationToken))
                return PaymentErrors.CustomerEmailAlreadyInUse(email);
        }

        customer.Update(command.Email, command.Name, command.Phone, command.Description);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated customer {Code}", customer.Code);

        return customer.ToDto();
    }
}
