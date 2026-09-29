using BuildingBlocks.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Application.Mappings;
using Payment.Domain;
using Payment.Domain.Entities;

namespace Payment.Application.Features.Command.CreateCustomer;

public class CreateCustomerHandler
{
    private readonly ICustomerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateCustomerCommand> _validator;
    private readonly ILogger<CreateCustomerHandler> _logger;

    public CreateCustomerHandler(
        ICustomerRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<CreateCustomerCommand> validator,
        ILogger<CreateCustomerHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<CustomerDto>> Handle(CreateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId}", nameof(CreateCustomerCommand), command.MerchantId);

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var email = command.Email.Trim().ToLowerInvariant();
        if (await _repository.ExistsByEmailAsync(command.MerchantId, email, cancellationToken))
            return PaymentErrors.CustomerEmailAlreadyInUse(email);

        var customer = new Customer(
            Guid.NewGuid(),
            command.MerchantId,
            Customer.NewCode(),
            email,
            command.Name,
            command.Phone,
            command.Description);

        await _repository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created customer {Code} for Merchant {MerchantId}", customer.Code, command.MerchantId);

        return customer.ToDto();
    }
}
