namespace Payment.Domain.Enums;

public enum TransactionType
{
    Authorization,
    Capture,
    Void,
    Refund
}