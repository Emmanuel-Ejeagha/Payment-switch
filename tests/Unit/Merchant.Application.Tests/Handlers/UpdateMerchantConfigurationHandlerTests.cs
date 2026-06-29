using Merchant.Application.Features.Commands.UpdateMerchantConfig;


namespace Merchant.Application.Tests.Handlers;

public class UpdateMerchantConfigurationHandlerTests
{
    private readonly Mock<IMerchantRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IValidator<UpdateMerchantConfigurationCommand>> _validatorMock = new();
    private readonly UpdateMerchantConfigurationHandler _handler;

    public UpdateMerchantConfigurationHandlerTests()
    {
        _handler = new UpdateMerchantConfigurationHandler(_repoMock.Object, _uowMock.Object, _dispatcherMock.Object, _validatorMock.Object);
    }

    [Fact]
    public async Task Handle_ActiveMerchant_ShouldUpdate()
    {
        var merchant = CreateActiveMerchant();
        var command = new UpdateMerchantConfigurationCommand(merchant.Id, "https://hook.com", new List<string> { "card" });
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://hook.com", merchant.WebhookUrl!.Value);
        Assert.Contains("card", merchant.EnabledPaymentMethods);
    }

    [Fact]
    public async Task Handle_SuspendedMerchant_ShouldFail()
    {
        var merchant = CreateActiveMerchant();
        merchant.Suspend();
        var command = new UpdateMerchantConfigurationCommand(merchant.Id, null, null);
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.ConfigurationUpdateFailed", result.Errors[0].Code);
    }

    private MerchantEntity CreateActiveMerchant()
    {
        var m = new MerchantEntity(Guid.NewGuid(), new BusinessName("Test"), new MerchantEmail("t@t.com"));
        m.Activate();
        m.ClearDomainEvents();
        return m;
    }

    private void SetupValidatorSuccess(UpdateMerchantConfigurationCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
}