using BuildingBlocks.Shared.Aggregate;
using Identity.Domain.DomainEvents;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Entities;

public class User : AggregateRoot
{
    public Email Email { get; private set; } = null!;
    public PasswordHash PasswordHash { get; private set; } = null!;
    public FullName FullName { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public bool EmailConfirmed { get; private set; }
    public DateTime? EmailVerifiedAt { get; private set; }
    public string? EmailVerificationTokenHash { get; private set; }
    public DateTime? EmailVerificationTokenExpiresAt { get; private set; }
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

    /// <summary>
    /// Registers a new verification token for an unconfirmed email, replacing any
    /// outstanding token (so a resend invalidates the previous one).
    /// </summary>
    public void InitiateEmailVerification(string tokenHash, DateTime expiresAtUtc)
    {
        if (EmailConfirmed)
            throw new InvalidOperationException("Email is already confirmed.");

        EmailVerificationTokenHash = tokenHash ?? throw new ArgumentNullException(nameof(tokenHash));
        EmailVerificationTokenExpiresAt = expiresAtUtc;
    }

    /// <summary>
    /// Attempts to confirm the email with the supplied hashed token.
    /// The token is single-use: a successful confirmation clears it.
    /// </summary>
    public EmailVerificationResult VerifyEmail(string tokenHash)
    {
        if (EmailConfirmed)
            return EmailVerificationResult.AlreadyConfirmed;

        if (string.IsNullOrEmpty(EmailVerificationTokenHash))
            return EmailVerificationResult.NoToken;

        if (!string.Equals(EmailVerificationTokenHash, tokenHash, StringComparison.Ordinal))
            return EmailVerificationResult.InvalidToken;

        if (EmailVerificationTokenExpiresAt is null || EmailVerificationTokenExpiresAt < DateTime.UtcNow)
            return EmailVerificationResult.TokenExpired;

        EmailConfirmed = true;
        EmailVerifiedAt = DateTime.UtcNow;
        EmailVerificationTokenHash = null;
        EmailVerificationTokenExpiresAt = null;
        return EmailVerificationResult.Success;
    }

    /// <summary>
    /// Marks an account verified without going through the token flow
    /// (e.g. seeded bootstrap admin).
    /// </summary>
    public void MarkEmailConfirmed()
    {
        EmailConfirmed = true;
        EmailVerifiedAt = DateTime.UtcNow;
        EmailVerificationTokenHash = null;
        EmailVerificationTokenExpiresAt = null;
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

    public ApiKey GenerateApiKey(string keyHash, string environment)
    {
        var apiKey = new ApiKey(keyHash, environment);
        _apiKeys.Add(apiKey);
        AddDomainEvent(new ApiKeyGeneratedDomainEvent(Id, Guid.Empty, environment));
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

    public TokenValue AddRefreshToken(string tokenHash, DateTime expiresAt)
    {
        var token = new TokenValue(tokenHash, expiresAt);
        _refreshTokens.Add(token);
        return token;
    }

    public void RevokeRefreshToken(string tokenHash)
    {
        var token = _refreshTokens.FirstOrDefault(t => t.Value == tokenHash);
        if (token != null)
            token.Revoke();
    }

    public void RevokeAllRefreshTokens()
    {
        foreach (var token in _refreshTokens)
            token.Revoke();
    }
}