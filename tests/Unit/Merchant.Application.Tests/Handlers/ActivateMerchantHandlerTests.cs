using Merchant.Application.Features.Commands.ActivateMerchant;


namespace Merchant.Application.Tests.Handlers;

public class ActivateMerchantHandlerTests
{
    private readonly Mock<IMerchantRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IValidator<ActivateMerchantCommand>> _validatorMock = new();
    private readonly Mock<ILogger<ActivateMerchantHandler>> _loggerMock = new();
    private readonly ActivateMerchantHandler _handler;

    public ActivateMerchantHandlerTests()
    {
        _handler = new ActivateMerchantHandler(_repoMock.Object, _uowMock.Object, _dispatcherMock.Object, _validatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_PendingMerchant_ShouldActivate()
    {
        var merchant = CreatePendingMerchant();
        var command = new ActivateMerchantCommand(merchant.Id);
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(MerchantStatus.Active, merchant.Status);
    }

    [Fact]
    public async Task Handle_AlreadyActive_ShouldFail()
    {
        var merchant = CreatePendingMerchant();
        merchant.Activate();
        var command = new ActivateMerchantCommand(merchant.Id);
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.InvalidStatusTransition", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_MerchantNotFound_ShouldFail()
    {
        var command = new ActivateMerchantCommand(Guid.NewGuid());
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((MerchantEntity?)null);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.MerchantNotFound", result.Errors[0].Code);
    }

    private MerchantEntity CreatePendingMerchant() =>
        new(Guid.NewGuid(), new BusinessName("Test"), new MerchantEmail("test@test.com"));

    private void SetupValidatorSuccess(ActivateMerchantCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
}