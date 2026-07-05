using BuildingBlocks.Shared.Results;

namespace Payment.Domain;

public static class PaymentErrors
{
    public static Error InvalidAmount =>
        new("Payment.InvalidAmount", "Amount must be greater than zero.");

    public static Error InvalidCurrency =>
        new("Payment.InvalidCurrency", "Currency must be a valid 3-letter ISO code.");

    public static Error IdempotencyKeyViolation(string key) =>
        new("Payment.IdempotencyKeyViolation", $"Payment intent with idempotency key '{key}' already exists.");

    public static Error InvalidStatusTransition(string current, string target) =>
        new("Payment.InvalidStatusTransition", $"Cannot transition from '{current}' to '{target}'.");

    public static Error CaptureExceedsAuthorized(decimal attempted, decimal authorized) =>
        new("Payment.CaptureExceedsAuthorized", $"Capture amount {attempted} exceeds authorized amount {authorized}.");

    public static Error RefundExceedsCaptured(decimal attempted, decimal captured) =>
        new("Payment.RefundExceedsCaptured", $"Refund amount {attempted} exceeds captured amount {captured}.");

    public static Error PaymentIntentNotFound(Guid intentId) =>
        new("Payment.PaymentIntentNotFound", $"Payment intent with Id '{intentId}' not found.");
}