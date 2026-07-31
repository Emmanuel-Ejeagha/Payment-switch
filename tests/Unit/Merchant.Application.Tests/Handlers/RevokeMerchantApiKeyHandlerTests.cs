using Merchant.Application.Features.Commands.RevokeMerchantApiKey;

namespace Merchant.Application.Tests.Handlers;

public class RevokeMerchantApiKeyHandlerTests
{
    private readonly Mock<IMerchantRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IValidator<RevokeMerchantApiKeyCommand>> _validatorMock = new();
    private readonly Mock<ILogger<RevokeMerchantApiKeyHandler>> _loggerMock = new();
    private readonly RevokeMerchantApiKeyHandler _handler;

    public RevokeMerchantApiKeyHandlerTests()
    {
        _handler = new RevokeMerchantApiKeyHandler(_repoMock.Object, _uowMock.Object, _validatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingKey_ShouldRevoke()
    {
        var merchant = CreateActiveMerchantWithKey(out var keyId);
        var command = new RevokeMerchantApiKeyCommand(merchant.Id, keyId);
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdWithApiKeysAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.NotNull(merchant.ApiKeys.Single().RevokedAt);
    }

    [Fact]
    public async Task Handle_MissingKey_ShouldFail()
    {
        var merchant = CreateActiveMerchantWithKey(out _);
        var command = new RevokeMerchantApiKeyCommand(merchant.Id, Guid.NewGuid());
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdWithApiKeysAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.ApiKeyNotFound", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_MerchantNotFound_ShouldFail()
    {
        var command = new RevokeMerchantApiKeyCommand(Guid.NewGuid(), Guid.NewGuid());
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdWithApiKeysAsync(command.MerchantId, It.IsAny<CancellationToken>())).ReturnsAsync((MerchantEntity?)null);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.MerchantNotFound", result.Errors[0].Code);
    }

    private MerchantEntity CreateActiveMerchantWithKey(out Guid keyId)
    {
        var m = new MerchantEntity(Guid.NewGuid(), new BusinessName("Test"), new MerchantEmail("t@t.com"));
        m.Activate();
        var key = m.GenerateApiKey("hash", "sk_test_", "test");
        keyId = key.Id;
        m.ClearDomainEvents();
        return m;
    }

    private void SetupValidatorSuccess(RevokeMerchantApiKeyCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
}
