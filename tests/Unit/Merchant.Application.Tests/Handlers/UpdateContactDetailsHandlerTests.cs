using Merchant.Application.Auth;
using Merchant.Application.Features.Commands.UpdateContactDetails;


namespace Merchant.Application.Tests.Handlers;

public class UpdateContactDetailsHandlerTests
{
    private readonly Mock<IMerchantRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IValidator<UpdateContactDetailsCommand>> _validatorMock = new();
    private readonly Mock<ILogger<UpdateContactDetailsHandler>> _loggerMock = new();
    private readonly UpdateContactDetailsHandler _handler;

    public UpdateContactDetailsHandlerTests()
    {
        _handler = new UpdateContactDetailsHandler(_repoMock.Object, _uowMock.Object, _validatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_Owner_ShouldPersistContactDetails()
    {
        var merchant = CreateMerchant();
        var command = new UpdateContactDetailsCommand(merchant.Id, "+2348000000000", "1 Test Street", "Ada", OwnerCaller(merchant));
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.NotNull(merchant.ContactDetails);
        Assert.Equal("+2348000000000", merchant.ContactDetails!.Phone);
        Assert.Equal("Ada", merchant.ContactDetails.ContactPerson);
    }

    [Fact]
    public async Task Handle_NonOwner_ShouldFail()
    {
        var merchant = CreateMerchant();
        var command = new UpdateContactDetailsCommand(merchant.Id, "+2348000000000", null, null, new CallerContext(Guid.NewGuid(), null, false));
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.Unauthorized", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_MerchantNotFound_ShouldFail()
    {
        var command = new UpdateContactDetailsCommand(Guid.NewGuid(), null, null, null, OwnerCaller(CreateMerchant()));
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

    private void SetupValidatorSuccess(UpdateContactDetailsCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
}