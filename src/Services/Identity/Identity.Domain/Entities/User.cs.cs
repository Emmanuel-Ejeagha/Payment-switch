using BuildingBlocks.Shared.Aggregate;
using Identity.Domain.DomainEvents;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Entities;

public class User : AggregateRoot
{
    public Email Email { get; private set; }
    public PasswordHash PasswordHash { get; private set; }
    public FullName FullName { get; private set; }
    public bool IsActive { get; private set; }
    private readonly List<string> _roles = new();
    private readonly List<TokenValue> _refreshTokens = new();
    private readonly List<ApiKey> _apiKeys = new();
    public IReadOnlyList<string> Roles => _roles.AsReadOnly();
    public IReadOnlyList<TokenValue> RefreshTokens => _refreshTokens.AsReadOnly();
    public IReadOnlyList<ApiKey> ApiKeys => _apiKeys.AsReadOnly();

    private User() : base() { }

    public User(Guid id, Email email, PasswordHash passwordHash, FullName fullName) : base(id)
    {
        Email = email ?? throw new ArgumentNullException(nameof(email));
        PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
        IsActive = true;
        _roles = new List<string> { "Merchant" }; // default role
        AddDomainEvent(new UserRegisteredDomainEvent(Id, email.Value, fullName.Value));
    }

    public void ChangePassword(PasswordHash newPasswordHash)
    {
        PasswordHash = newPasswordHash ?? throw new ArgumentNullException(nameof(newPasswordHash));
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void AddRole(string role)
    {
        if (!_roles.Contains(role))
            _roles.Add(role);
    }

    public void RemoveRole(string role)
    {
        _roles.Remove(role);
    }

    public ApiKey GenerateApiKey(Guid keyId, string keyHash, string environment)
    {
        var apiKey = new ApiKey(keyId, keyHash, environment);
        _apiKeys.Add(apiKey);
        AddDomainEvent(new ApiKeyGeneratedDomainEvent(Id, keyId, environment));
        return apiKey;
    }

    public void RevokeApiKey(Guid keyId)
    {
        var apiKey = _apiKeys.FirstOrDefault(k => k.Id == keyId);
        if (apiKey == null)
            throw new InvalidOperationException("API key not found.");
        apiKey.Revoke();
        AddDomainEvent(new ApiKeyRevokedDomainEvent(Id, keyId));
    }

    public TokenValue AddRefreshToken(string tokenValue, DateTime expiresAt)
    {
        var token = new TokenValue(tokenValue, expiresAt);
        _refreshTokens.Add(token);
        return token;
    }

    public void RevokeRefreshToken(string tokenValue)
    {
        var token = _refreshTokens.FirstOrDefault(t => t.Value == tokenValue);
        if (token != null)
            token.Revoke();
    }
}