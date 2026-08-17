using Merchant.Application.Auth;
using Merchant.Application.Features.Commands.OnboardMerchant;

namespace Merchant.Application.Tests.Handlers;

public class OnboardMerchantHandlerTests
{
    private readonly Mock<IMerchantRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IValidator<OnboardMerchantCommand>> _validatorMock = new();
    private readonly Mock<ILogger<OnboardMerchantHandler>> _loggerMock = new();
    private readonly OnboardMerchantHandler _handler;

    public OnboardMerchantHandlerTests()
    {
        _handler = new OnboardMerchantHandler(_repoMock.Object, _uowMock.Object, _validatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_VerifiedOwner_ShouldCreateMerchant()
    {
        var ownerId = Guid.NewGuid();
        var command = new OnboardMerchantCommand("Acme Corp", "acme@test.com", new CallerContext(ownerId, "acme@test.com", false, EmailVerified: true));
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value!.MerchantId);
        _repoMock.Verify(r => r.AddAsync(It.Is<MerchantEntity>(m => m.BusinessName.Value == command.BusinessName && m.OwnerId == ownerId), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AnonymousCaller_ShouldBeUnauthorized()
    {
        var command = new OnboardMerchantCommand("Acme Corp", "acme@test.com", CallerContext.Anonymous);
        SetupValidatorSuccess(command);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.Unauthorized", result.Errors[0].Code);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<MerchantEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnverifiedCaller_ShouldFail()
    {
        var ownerId = Guid.NewGuid();
        var command = new OnboardMerchantCommand("Acme Corp", "acme@test.com", new CallerContext(ownerId, "acme@test.com", false));
        SetupValidatorSuccess(command);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.EmailNotVerified", result.Errors[0].Code);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<MerchantEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmailMismatch_ShouldBeUnauthorized()
    {
        var ownerId = Guid.NewGuid();
        var command = new OnboardMerchantCommand("Acme Corp", "other@test.com", new CallerContext(ownerId, "acme@test.com", false, EmailVerified: true));
        SetupValidatorSuccess(command);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.Unauthorized", result.Errors[0].Code);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<MerchantEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ShouldFail()
    {
        var ownerId = Guid.NewGuid();
        var command = new OnboardMerchantCommand("Acme", "dup@test.com", new CallerContext(ownerId, "dup@test.com", false, EmailVerified: true));
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "Merchant.EmailAlreadyInUse");
    }

    [Fact]
    public async Task Handle_InvalidCommand_ShouldReturnValidationErrors()
    {
        var command = new OnboardMerchantCommand("", "", new CallerContext(Guid.NewGuid(), "acme@test.com", false, EmailVerified: true));
        SetupValidatorFailure(command, "BusinessName", "Business name is required.");

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "BusinessName");
    }

    private void SetupValidatorSuccess(OnboardMerchantCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());

    private void SetupValidatorFailure(OnboardMerchantCommand command, string property, string error) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult(new[] { new ValidationFailure(property, error) }));
}