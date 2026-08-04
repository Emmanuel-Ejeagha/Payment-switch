using FluentValidation;

namespace Payment.Application.Features.Command.UpdateCustomer;

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.MerchantId).NotEmpty();
        RuleFor(x => x.Email).EmailAddress().MaximumLength(320).When(x => x.Email is not null);
        RuleFor(x => x.Name).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(32);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
