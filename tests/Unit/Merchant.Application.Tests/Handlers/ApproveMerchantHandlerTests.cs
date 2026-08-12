using Merchant.Application.Features.Commands.ApproveMerchant;


namespace Merchant.Application.Tests.Handlers;

public class ApproveMerchantHandlerTests
{
    private readonly Mock<IMerchantRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IValidator<ApproveMerchantCommand>> _validatorMock = new();
    private readonly Mock<ILogger<ApproveMerchantHandler>> _loggerMock = new();
    private readonly ApproveMerchantHandler _handler;

    public ApproveMerchantHandlerTests()
    {
        _handler = new ApproveMerchantHandler(_repoMock.Object, _uowMock.Object, _dispatcherMock.Object, _validatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_PendingMerchant_ShouldApprove()
    {
        var merchant = CreatePendingMerchant();
        var command = new ApproveMerchantCommand(merchant.Id);
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(MerchantStatus.Approved, merchant.Status);
    }

    [Fact]
    public async Task Handle_AlreadyApproved_ShouldFail()
    {
        var merchant = CreatePendingMerchant();
        merchant.Approve();
        var command = new ApproveMerchantCommand(merchant.Id);
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.InvalidStatusTransition", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_MerchantNotFound_ShouldFail()
    {
        var command = new ApproveMerchantCommand(Guid.NewGuid());
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((MerchantEntity?)null);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.MerchantNotFound", result.Errors[0].Code);
    }

    private MerchantEntity CreatePendingMerchant() =>
        new(Guid.NewGuid(), new BusinessName("Test"), new MerchantEmail("test@test.com"));

    private void SetupValidatorSuccess(ApproveMerchantCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
}