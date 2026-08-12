using Merchant.Application.Auth;
using Merchant.Application.Features.Commands.UpdateSettlementInfo;


namespace Merchant.Application.Tests.Handlers;

public class UpdateSettlementInfoHandlerTests
{
    private readonly Mock<IMerchantRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IValidator<UpdateSettlementInfoCommand>> _validatorMock = new();
    private readonly Mock<ILogger<UpdateSettlementInfoHandler>> _loggerMock = new();
    private readonly UpdateSettlementInfoHandler _handler;

    public UpdateSettlementInfoHandlerTests()
    {
        _handler = new UpdateSettlementInfoHandler(_repoMock.Object, _uowMock.Object, _dispatcherMock.Object, _validatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_Owner_ShouldPersistSettlementInfo()
    {
        var merchant = CreateMerchant();
        var command = new UpdateSettlementInfoCommand(merchant.Id, "Acme Ltd", "0099887766", "Test Bank", "USD", "WEEKLY", OwnerCaller(merchant));
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.NotNull(merchant.SettlementInfo);
        Assert.Equal("Acme Ltd", merchant.SettlementInfo!.BankAccountName);
        Assert.Equal("0099887766", merchant.SettlementInfo.BankAccountNumber);
        Assert.Equal("WEEKLY", merchant.SettlementInfo.SettlementSchedule);
    }

    [Fact]
    public async Task Handle_NonOwner_ShouldFail()
    {
        var merchant = CreateMerchant();
        var command = new UpdateSettlementInfoCommand(merchant.Id, "Acme Ltd", "0099887766", "Test Bank", "USD", "DAILY", new CallerContext(Guid.NewGuid(), null, false));
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.Unauthorized", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_InvalidCurrency_ShouldReturnValidationFailure()
    {
        var merchant = CreateMerchant();
        var command = new UpdateSettlementInfoCommand(merchant.Id, "Acme Ltd", "0099887766", "Test Bank", "USDOLLAR", "DAILY", OwnerCaller(merchant));
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("SettlementCurrency", "Settlement currency must be a 3-letter ISO currency code.") }));

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("SettlementCurrency", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_MerchantNotFound_ShouldFail()
    {
        var command = new UpdateSettlementInfoCommand(Guid.NewGuid(), "Acme Ltd", "0099887766", "Test Bank", "USD", "DAILY", OwnerCaller(CreateMerchant()));
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((MerchantEntity?)null);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.MerchantNotFound", result.Errors[0].Code);
    }

    private CallerContext OwnerCaller(MerchantEntity merchant) => new(merchant.OwnerId, null, false);

    private MerchantEntity CreateMerchant()
    {
        var ownerId = Guid.NewGuid();
        return new MerchantEntity(Guid.NewGuid(), ownerId, new BusinessName("Test"), new MerchantEmail("t@t.com"));
    }

    private void SetupValidatorSuccess(UpdateSettlementInfoCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
}