using BuildingBlocks.Shared.Results;

namespace Payment.Domain;

public static class PaymentErrors
{
    public static Error InvalidAmount =>
        new("Payment.InvalidAmount", "Amount must be greater than zero.");

    public static Error InvalidCurrency =>
        new("Payment.InvalidCurrency", "Currency must be a valid 3-letter ISO code.");

    public static Error InvalidStatusTransition(string current, string target) =>
        new("Payment.InvalidStatusTransition", $"Cannot transition from '{current}' to '{target}'.");

    public static Error CaptureExceedsAuthorized(long attempted, long authorized) =>
        new("Payment.CaptureExceedsAuthorized", $"Capture amount {attempted} exceeds authorized amount {authorized}.");

    public static Error RefundExceedsCaptured(long attempted, long captured) =>
        new("Payment.RefundExceedsCaptured", $"Refund amount {attempted} exceeds captured amount {captured}.");

    public static Error PaymentIntentNotFound(Guid intentId) =>
        new("Payment.PaymentIntentNotFound", $"Payment intent with Id '{intentId}' not found.");

    public static Error ConcurrencyConflict =>
        new("Payment.ConcurrencyConflict", "This payment was modified concurrently. Please retry.");

    public static Error CustomerNotFound(Guid customerId) =>
        new("Payment.CustomerNotFound", $"Customer with Id '{customerId}' not found.");

    public static Error CustomerEmailAlreadyInUse(string email) =>
        new("Payment.CustomerEmailAlreadyInUse", $"A customer with email '{email}' already exists for this merchant.");
}