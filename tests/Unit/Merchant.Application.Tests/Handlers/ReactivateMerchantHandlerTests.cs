using Merchant.Application.Features.Commands.ReactivateMerchant;


namespace Merchant.Application.Tests.Handlers;

public class ReactivateMerchantHandlerTests
{
    private readonly Mock<IMerchantRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IValidator<ReactivateMerchantCommand>> _validatorMock = new();
    private readonly Mock<ILogger<ReactivateMerchantHandler>> _loggerMock = new();
    private readonly ReactivateMerchantHandler _handler;

    public ReactivateMerchantHandlerTests()
    {
        _handler = new ReactivateMerchantHandler(_repoMock.Object, _uowMock.Object, _validatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_SuspendedMerchant_ShouldReactivate()
    {
        var merchant = CreateSuspendedMerchant();
        var command = new ReactivateMerchantCommand(merchant.Id);
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(MerchantStatus.Active, merchant.Status);
    }

    [Fact]
    public async Task Handle_ActiveMerchant_ShouldFail()
    {
        var merchant = CreateSuspendedMerchant();
        merchant.Reactivate();
        var command = new ReactivateMerchantCommand(merchant.Id);
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.InvalidStatusTransition", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_MerchantNotFound_ShouldFail()
    {
        var command = new ReactivateMerchantCommand(Guid.NewGuid());
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((MerchantEntity?)null);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.MerchantNotFound", result.Errors[0].Code);
    }

    private MerchantEntity CreateSuspendedMerchant()
    {
        var merchant = new MerchantEntity(Guid.NewGuid(), new BusinessName("Test"), new MerchantEmail("test@test.com"));
        merchant.Approve();
        merchant.Activate();
        merchant.Suspend();
        return merchant;
    }

    private void SetupValidatorSuccess(ReactivateMerchantCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
}