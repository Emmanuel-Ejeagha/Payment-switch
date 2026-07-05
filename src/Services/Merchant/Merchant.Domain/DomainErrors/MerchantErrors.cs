using BuildingBlocks.Shared.Results;

namespace Merchant.Domain.DomainErrors;

public static class MerchantErrors
{
    public static Error MerchantNotFound(Guid merchantId) =>
        new("Merchant.MerchantNotFound", $"Merchant with Id '{merchantId}' not found.");

    public static Error AlreadyOnboarded(Guid merchantId) =>
        new("Merchant.AlreadyOnboarded", $"Merchant with Id '{merchantId}' is already onboarded.");

    public static Error InvalidStatusTransition(string current, string target) =>
        new("Merchant.InvalidStatusTransition", $"Cannot transition from '{current}' to '{target}'.");

    public static Error InvalidEmailFormat(string email) =>
        new("Merchant.InvalidEmailFormat", $"Email '{email}' has an invalid format.");

    public static Error EmailAlreadyInUse(string email) =>
        new("Merchant.EmailAlreadyInUse", $"Email '{email}' is already used by another merchant.");
}