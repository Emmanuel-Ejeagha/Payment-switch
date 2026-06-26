using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using FluentValidation.Results;
using Identity.Application.Interfaces;
using Identity.Domain.DomainErrors;

namespace Identity.Application.Commands.Auth.Login;

public class LoginHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IValidator<LoginCommand> _validator;

    public LoginHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenService tokenService, IUnitOfWork unitOfWork, IDomainEventDispatcher dispatcher, IValidator<LoginCommand> validator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _validator = validator;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user == null)
            return IdentityErrors.InvalidCredentials;

        if (!_passwordHasher.Verify(command.Password, user.PasswordHash))
            return IdentityErrors.InvalidCredentials;

        if (!user.IsActive)
            return new Error("Identity.UserInactive", "User account is deactivated.");

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var expiresIn = 3600; // 1 hour, should come from config but hardcoded for now

        user.AddRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.DispatchAsync(user.DomainEvents, cancellationToken);

        return new LoginResponse(accessToken, refreshToken, expiresIn);
    }
}