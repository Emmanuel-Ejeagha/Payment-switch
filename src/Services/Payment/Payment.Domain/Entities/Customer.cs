using BuildingBlocks.Shared.Aggregate;

namespace Payment.Domain.Entities;

public class Customer : AggregateRoot
{
    public Guid MerchantId { get; private set; }
    public string Code { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string? Name { get; private set; }
    public string? Phone { get; private set; }
    public string? Description { get; private set; }
    public bool Deleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Customer() : base() { }

    public Customer(
        Guid id,
        Guid merchantId,
        string code,
        string email,
        string? name = null,
        string? phone = null,
        string? description = null) : base(id)
    {
        if (merchantId == Guid.Empty)
            throw new ArgumentException("MerchantId is required.", nameof(merchantId));

        MerchantId = merchantId;
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Email = NormalizeEmail(email);
        Name = name;
        Phone = phone;
        Description = description;
        Deleted = false;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Applies a partial update. Null arguments leave the existing value untouched,
    /// matching the PATCH-style semantics the public API exposes.
    /// </summary>
    public void Update(string? email, string? name, string? phone, string? description)
    {
        if (Deleted)
            throw new InvalidOperationException("Cannot update a deleted customer.");

        if (email is not null) Email = NormalizeEmail(email);
        if (name is not null) Name = name;
        if (phone is not null) Phone = phone;
        if (description is not null) Description = description;

        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkDeleted()
    {
        if (Deleted) return;

        Deleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        return email.Trim().ToLowerInvariant();
    }

    public static string NewCode() => $"cus_{Guid.NewGuid().ToString("N")[..24]}";
}
