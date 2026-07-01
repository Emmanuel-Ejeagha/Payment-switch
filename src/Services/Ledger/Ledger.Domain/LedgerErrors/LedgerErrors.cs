using BuildingBlocks.Shared.Results;

namespace Ledger.Domain.DomainErrors;

public static class LedgerErrors
{
    public static Error AccountNotFound(Guid merchantId) =>
        new("Ledger.AccountNotFound", $"Ledger account for merchant '{merchantId}' not found.");

    public static Error InsufficientFunds =>
        new("Ledger.InsufficientFunds", "Insufficient funds for this operation.");

    public static Error AmountMustBePositive =>
        new("Ledger.AmountMustBePositive", "Amount must be greater than zero.");
}