using Identity.Application.Interfaces;
using Identity.Domain.ValueObjects;

namespace Identity.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    public PasswordHash Hash(string plainPassword)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
        return new PasswordHash(hash);
    }

    public bool Verify(string plainPassword, PasswordHash hash)
    {
        return BCrypt.Net.BCrypt.Verify(plainPassword, hash.Hash);
    }
}
