using Identity.Domain.ValueObjects;

namespace Identity.Application.Interfaces;

public interface IPasswordHasher
{
    PasswordHash Hash(string plainPassword);
    bool Verify(string plainPassword, PasswordHash hash);
}