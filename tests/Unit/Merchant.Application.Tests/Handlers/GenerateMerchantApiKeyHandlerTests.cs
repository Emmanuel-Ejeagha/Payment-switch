using Merchant.Application.Auth;
using Merchant.Application.Features.Commands.GenerateMerchantApiKey;

namespace Merchant.Application.Tests.Handlers;

public class GenerateMerchantApiKeyHandlerTests
{
    private readonly Mock<IMerchantRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IValidator<GenerateMerchantApiKeyCommand>> _validatorMock = new();
    private readonly Mock<ILogger<GenerateMerchantApiKeyHandler>> _loggerMock = new();
    private readonly GenerateMerchantApiKeyHandler _handler;

    public GenerateMerchantApiKeyHandlerTests()
    {
        _handler = new GenerateMerchantApiKeyHandler(_repoMock.Object, _uowMock.Object, _validatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ActiveMerchant_ShouldGenerateKey()
    {
        var merchant = CreateActiveMerchant();
        var command = new GenerateMerchantApiKeyCommand(merchant.Id, "test", OwnerCaller(merchant));
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdWithApiKeysAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.StartsWith("sk_test_", result.Value!.PlainTextKey);
        Assert.Single(merchant.ApiKeys);
        Assert.Equal("test", merchant.ApiKeys[0].Environment);
        Assert.NotEqual(result.Value!.PlainTextKey, merchant.ApiKeys[0].KeyHash);
    }

    [Fact]
    public async Task Handle_LiveEnvironment_ShouldUseLivePrefix()
    {
        var merchant = CreateActiveMerchant();
        var command = new GenerateMerchantApiKeyCommand(merchant.Id, "live", OwnerCaller(merchant));
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdWithApiKeysAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.StartsWith("sk_live_", result.Value!.PlainTextKey);
    }

    [Fact]
    public async Task Handle_NonActiveMerchant_ShouldFail()
    {
        var merchant = new MerchantEntity(Guid.NewGuid(), Guid.NewGuid(), new BusinessName("Test"), new MerchantEmail("t@t.com"));
        var command = new GenerateMerchantApiKeyCommand(merchant.Id, "test", OwnerCaller(merchant));
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdWithApiKeysAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.NotActive", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_MerchantNotFound_ShouldFail()
    {
        var command = new GenerateMerchantApiKeyCommand(Guid.NewGuid(), "test", new CallerContext(Guid.NewGuid(), null, false));
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdWithApiKeysAsync(command.MerchantId, It.IsAny<CancellationToken>())).ReturnsAsync((MerchantEntity?)null);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.MerchantNotFound", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_NonOwner_ShouldFail()
    {
        var merchant = CreateActiveMerchant();
        var command = new GenerateMerchantApiKeyCommand(merchant.Id, "test", new CallerContext(Guid.NewGuid(), null, false));
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdWithApiKeysAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.Unauthorized", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_UnverifiedEmailCaller_ShouldFail()
    {
        var merchant = CreateActiveMerchant();
        var command = new GenerateMerchantApiKeyCommand(merchant.Id, "test", new CallerContext(merchant.OwnerId, null, false, false));
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdWithApiKeysAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.EmailNotVerified", result.Errors[0].Code);
        Assert.Empty(merchant.ApiKeys);
    }

    [Fact]
    public async Task Handle_InvalidEnvironment_ShouldReturnValidationErrors()
    {
        var command = new GenerateMerchantApiKeyCommand(Guid.NewGuid(), "prod", new CallerContext(Guid.NewGuid(), null, false));
        SetupValidatorFailure(command);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Environment", result.Errors[0].Code);
    }

    private CallerContext OwnerCaller(MerchantEntity merchant) => new(merchant.OwnerId, null, false, true);

    private MerchantEntity CreateActiveMerchant()
    {
        var ownerId = Guid.NewGuid();
        var m = new MerchantEntity(Guid.NewGuid(), ownerId, new BusinessName("Test"), new MerchantEmail("t@t.com"));
        m.Activate();
        m.ClearDomainEvents();
        return m;
    }

    private void SetupValidatorSuccess(GenerateMerchantApiKeyCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());

    private void SetupValidatorFailure(GenerateMerchantApiKeyCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Environment", "Environment must be 'live' or 'test'.") }));
}
