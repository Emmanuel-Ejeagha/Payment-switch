using BuildingBlocks.Shared;

namespace Merchant.Domain.ValueObjects;

/// <summary>
/// Operational contact details for a merchant (phone / address / contact person).
/// </summary>
public class ContactDetails : ValueObject
{
    public string? Phone { get; }
    public string? Address { get; }
    public string? ContactPerson { get; }

    public ContactDetails(string? phone, string? address, string? contactPerson)
    {
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        ContactPerson = string.IsNullOrWhiteSpace(contactPerson) ? null : contactPerson.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Phone;
        yield return Address;
        yield return ContactPerson;
    }
}