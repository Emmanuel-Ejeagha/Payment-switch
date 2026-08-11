namespace Identity.Domain.Entities;

public enum PasswordResetResult
{
    Success,
    NoToken,
    InvalidToken,
    TokenExpired
}
