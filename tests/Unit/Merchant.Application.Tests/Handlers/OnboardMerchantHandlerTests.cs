using Merchant.Application.Features.Commands.OnboardMerchant;

namespace Merchant.Application.Tests.Handlers;

public class OnboardMerchantHandlerTests
{
    private readonly Mock<IMerchantRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IValidator<OnboardMerchantCommand>> _validatorMock = new();
    private readonly OnboardMerchantHandler _handler;

    public OnboardMerchantHandlerTests()
    {
        _handler = new OnboardMerchantHandler(_repoMock.Object, _uowMock.Object, _dispatcherMock.Object, _validatorMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateMerchant()
    {
        var command = new OnboardMerchantCommand("Acme Corp", "acme@test.com");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value!.MerchantId);
        _repoMock.Verify(r => r.AddAsync(It.Is<MerchantEntity>(m => m.BusinessName.Value == command.BusinessName), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<IReadOnlyList<DomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ShouldFail()
    {
        var command = new OnboardMerchantCommand("Acme", "dup@test.com");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "Merchant.EmailAlreadyInUse");
    }

    [Fact]
    public async Task Handle_InvalidCommand_ShouldReturnValidationErrors()
    {
        var command = new OnboardMerchantCommand("", "");
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