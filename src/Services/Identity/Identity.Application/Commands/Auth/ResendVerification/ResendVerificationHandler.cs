using BuildingBlocks.Shared.Email;
using BuildingBlocks.Shared.Results;
using BuildingBlocks.Shared.Security;
using FluentValidation;
using Identity.Application.Configuration;
using Identity.Application.Interfaces;
using Identity.Application.Services;
using Identity.Domain.DomainErrors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Application.Commands.Auth.ResendVerification;

public class ResendVerificationHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailVerificationTokenFactory _tokenFactory;
    private readonly IEmailSender _emailSender;
    private readonly EmailVerificationOptions _options;
    private readonly IValidator<ResendVerificationCommand> _validator;
    private readonly ILogger<ResendVerificationHandler> _logger;

    public ResendVerificationHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IEmailVerificationTokenFactory tokenFactory,
        IEmailSender emailSender,
        IOptions<EmailVerificationOptions> options,
        IValidator<ResendVerificationCommand> validator,
        ILogger<ResendVerificationHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _tokenFactory = tokenFactory;
        _emailSender = emailSender;
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
            // Respond identically for unknown addresses so the endpoint cannot be
            // used to enumerate which emails have accounts.
            _logger.LogWarning("Verification resend requested for unknown {Identifier}", DataMasker.MaskEmail(command.Email));
            return Result.Success();
        }

        if (user.EmailConfirmed)
            return IdentityErrors.EmailAlreadyVerified;

        var token = _tokenFactory.Generate(TimeSpan.FromHours(_options.TokenLifetimeHours));
        user.InitiateEmailVerification(token.Hash, token.ExpiresAtUtc);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var message = VerificationEmailBuilder.Build(user.Email.Value, token.PlainText, _options.Subject, _options.FrontendBaseUrl);
        var sendResult = await _emailSender.SendAsync(message, cancellationToken);
        if (sendResult.IsFailure)
            _logger.LogError("Failed to resend verification email to {Identifier}: {Errors}",
                DataMasker.MaskEmail(command.Email), string.Join("; ", sendResult.Errors.Select(e => e.Message)));

        return Result.Success();
    }
}
