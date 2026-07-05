namespace Identity.Application.DTOs;

public record ApiKeyDto(Guid KeyId, string Environment, DateTime CreatedAt, DateTime? RevokedAt);