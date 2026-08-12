namespace Merchant.Application.Features.Commands.UpdateContactDetails;

public class UpdateContactDetailsCommandValidator : AbstractValidator<UpdateContactDetailsCommand>
{
    public UpdateContactDetailsCommandValidator()
    {
        RuleFor(x => x.MerchantId).NotEmpty();
        RuleFor(x => x.Phone).MaximumLength(30).When(x => !string.IsNullOrEmpty(x.Phone));
        RuleFor(x => x.Address).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Address));
        RuleFor(x => x.ContactPerson).MaximumLength(200).When(x => !string.IsNullOrEmpty(x.ContactPerson));
    }
}