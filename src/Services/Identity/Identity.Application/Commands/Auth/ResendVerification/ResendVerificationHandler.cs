using BuildingBlocks.Shared.Results;
using BuildingBlocks.Shared.Security;
using FluentValidation;
using Identity.Application.Configuration;
using Identity.Application.Interfaces;
using Identity.Domain.DomainErrors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Application.Commands.Auth.ResendVerification;

public class ResendVerificationHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailVerificationTokenFactory _tokenFactory;
    private readonly EmailVerificationOptions _options;
    private readonly IValidator<ResendVerificationCommand> _validator;
    private readonly ILogger<ResendVerificationHandler> _logger;

    public ResendVerificationHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IEmailVerificationTokenFactory tokenFactory,
        IOptions<EmailVerificationOptions> options,
        IValidator<ResendVerificationCommand> validator,
        ILogger<ResendVerificationHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _tokenFactory = tokenFactory;
        _options = options.Value;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> Handle(ResendVerificationCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for {Identifier}", nameof(ResendVerificationCommand), DataMasker.MaskEmail(command.Email));
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user == null)
        {
            // Neutral response so the endpoint cannot be used to enumerate
            // which email addresses have accounts (same convention as
            // forgot-password). Nothing is sent and no state changes.
            _logger.LogWarning("Resend-verification requested for unknown {Identifier}", DataMasker.MaskEmail(command.Email));
            return Result.Success();
        }

        if (user.EmailConfirmed)
        {
            // Neutral as well: confirming "already verified" would disclose
            // that the address is registered AND verified.
            _logger.LogInformation("Resend-verification requested for already-verified {Identifier}", DataMasker.MaskEmail(command.Email));
            return Result.Success();
        }

        // Database-backed cooldown: holds across API instances (unlike the
        // per-instance Strict rate limiter, which remains as outer defense).
        // Rejected requests send nothing and rotate nothing.
        var cooldown = TimeSpan.FromSeconds(Math.Max(0, _options.ResendCooldownSeconds));
        if (user.LastVerificationEmailSentAtUtc is not null)
        {
            var elapsed = DateTime.UtcNow - user.LastVerificationEmailSentAtUtc.Value;
            if (elapsed < cooldown)
            {
                var retryAfter = (int)Math.Ceiling((cooldown - elapsed).TotalSeconds);
                _logger.LogWarning("Resend-verification throttled for {Identifier}; retry in {RetryAfter}s",
                    DataMasker.MaskEmail(command.Email), retryAfter);
                return IdentityErrors.VerificationResendThrottled(retryAfter);
            }
        }

        var token = _tokenFactory.Generate(TimeSpan.FromHours(_options.TokenLifetimeHours));
        user.InitiateEmailVerification(token.Hash, token.PlainText, token.ExpiresAtUtc);
        user.RecordVerificationEmailSent();

        // Delivery is event-driven (see RegisterUserHandler): the event raised
        // above is captured into the outbox by this SaveChanges.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
