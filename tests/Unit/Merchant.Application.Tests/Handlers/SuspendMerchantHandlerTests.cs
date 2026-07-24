namespace Merchant.Application.Tests.Handlers;

public class SuspendMerchantHandlerTests
{
    private readonly Mock<IMerchantRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IValidator<SuspendMerchantCommand>> _validatorMock = new();
    private readonly Mock<ILogger<SuspendMerchantHandler>> _loggerMock = new();
    private readonly SuspendMerchantHandler _handler;

    public SuspendMerchantHandlerTests()
    {
        _handler = new SuspendMerchantHandler(_repoMock.Object, _uowMock.Object, _dispatcherMock.Object, _validatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ActiveMerchant_ShouldSuspend()
    {
        var merchant = CreateActiveMerchant();
        var command = new SuspendMerchantCommand(merchant.Id);
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(MerchantStatus.Suspended, merchant.Status);
    }

    [Fact]
    public async Task Handle_AlreadySuspended_ShouldFail()
    {
        var merchant = CreateActiveMerchant();
        merchant.Suspend();
        var command = new SuspendMerchantCommand(merchant.Id);
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.InvalidStatusTransition", result.Errors[0].Code);
    }

    private MerchantEntity CreateActiveMerchant()
    {
        var m = new MerchantEntity(Guid.NewGuid(), new BusinessName("Test"), new MerchantEmail("test@test.com"));
        m.Activate();
        m.ClearDomainEvents();
        return m;
    }

    private void SetupValidatorSuccess(SuspendMerchantCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
}