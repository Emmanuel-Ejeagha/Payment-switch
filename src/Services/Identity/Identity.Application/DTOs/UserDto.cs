namespace Identity.Application.DTOs;

public record UserDto(Guid Id, string Email, string FullName, bool IsActive, bool EmailConfirmed, List<string> Roles);
