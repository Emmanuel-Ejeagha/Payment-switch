using BuildingBlocks.Shared.Results;

namespace Settlement.Domain.DomainErrors;

public static class SettlementErrors
{
    public static Error BatchAlreadyCompleted =>
        new("Settlement.BatchAlreadyCompleted", "Settlement batch is already completed.");

    public static Error DuplicateMerchant(Guid merchantId) =>
        new("Settlement.DuplicateMerchant", $"Merchant '{merchantId}' is already in the batch.");

    public static Error BatchNotFound(Guid batchId) =>
        new("Settlement.BatchNotFound", $"Settlement batch with Id '{batchId}' not found.");
}