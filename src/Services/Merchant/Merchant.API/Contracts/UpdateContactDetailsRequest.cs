namespace Merchant.API.Contracts;

public record UpdateContactDetailsRequest(string? Phone, string? Address, string? ContactPerson);