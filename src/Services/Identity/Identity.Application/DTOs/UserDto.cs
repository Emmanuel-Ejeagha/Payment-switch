namespace Identity.Application.DTOs;

public record UserDto(Guid Id, string Email, string FullName, bool IsActive, List<string> Roles);
