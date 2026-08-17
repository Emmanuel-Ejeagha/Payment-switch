using BuildingBlocks.Shared.Email;
using BuildingBlocks.Shared.Results;
using BuildingBlocks.Shared.Security;
using FluentValidation;
using Identity.Application.Configuration;
using Identity.Application.Exceptions;
using Identity.Application.Interfaces;
using Identity.Application.Services;
using Identity.Domain.DomainErrors;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Application.Commands.Auth.Register;

public class RegisterUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailVerificationTokenFactory _tokenFactory;
    private readonly IEmailSender _emailSender;
    private readonly EmailVerificationOptions _options;
    private readonly IValidator<RegisterUserCommand> _validator;
    private readonly ILogger<RegisterUserHandler> _logger;

    public RegisterUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IEmailVerificationTokenFactory tokenFactory,
        IEmailSender emailSender,
        IOptions<EmailVerificationOptions> options,
        IValidator<RegisterUserCommand> validator,
        ILogger<RegisterUserHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _tokenFactory = tokenFactory;
        _emailSender = emailSender;
        _options = options.Value;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<RegisterUserResponse>> Handle(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for {Identifier}", nameof(RegisterUserCommand), DataMasker.MaskEmail(command.Email));
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
            return validationResult.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        if (await _userRepository.ExistsByEmailAsync(command.Email, cancellationToken))
            return IdentityErrors.EmailAlreadyInUse(command.Email);

        var email = new Email(command.Email);
        var passwordHash = _passwordHasher.Hash(command.Password);
        var fullName = new FullName(command.FullName);

        var user = new User(Guid.NewGuid(), email, passwordHash, fullName);

        var token = _tokenFactory.Generate(TimeSpan.FromHours(_options.TokenLifetimeHours));
        user.InitiateEmailVerification(token.Hash, token.ExpiresAtUtc);

        await _userRepository.AddAsync(user, cancellationToken);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (EmailConflictException)
        {
            // Backstop for a duplicate that raced past the existence check
            // (TASK-014): map to the same graceful 409 as the primary path.
            return IdentityErrors.EmailAlreadyInUse(command.Email);
        }

        var message = VerificationEmailBuilder.Build(user.Email.Value, token.PlainText, _options.Subject, _options.FrontendBaseUrl);
        var sendResult = await _emailSender.SendAsync(message, cancellationToken);
        if (sendResult.IsFailure)
            _logger.LogError("Failed to send verification email to {Identifier}: {Errors}",
                DataMasker.MaskEmail(command.Email), string.Join("; ", sendResult.Errors.Select(e => e.Message)));

        return new RegisterUserResponse(user.Id);
    }
}
