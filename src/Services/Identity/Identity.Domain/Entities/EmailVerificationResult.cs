namespace Identity.Domain.Entities;

public enum EmailVerificationResult
{
    Success,
    AlreadyConfirmed,
    InvalidToken,
    TokenExpired,
    NoToken
}
