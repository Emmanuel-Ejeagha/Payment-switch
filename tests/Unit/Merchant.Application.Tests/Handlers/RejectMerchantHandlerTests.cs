using Merchant.Application.Features.Commands.RejectMerchant;


namespace Merchant.Application.Tests.Handlers;

public class RejectMerchantHandlerTests
{
    private readonly Mock<IMerchantRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IValidator<RejectMerchantCommand>> _validatorMock = new();
    private readonly Mock<ILogger<RejectMerchantHandler>> _loggerMock = new();
    private readonly RejectMerchantHandler _handler;

    public RejectMerchantHandlerTests()
    {
        _handler = new RejectMerchantHandler(_repoMock.Object, _uowMock.Object, _dispatcherMock.Object, _validatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_PendingMerchant_ShouldRejectWithReason()
    {
        var merchant = CreatePendingMerchant();
        var command = new RejectMerchantCommand(merchant.Id, "Missing documentation");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(MerchantStatus.Rejected, merchant.Status);
        Assert.Equal("Missing documentation", merchant.RejectionReason);
    }

    [Fact]
    public async Task Handle_ApprovedMerchant_ShouldFail()
    {
        var merchant = CreatePendingMerchant();
        merchant.Approve();
        var command = new RejectMerchantCommand(merchant.Id, "Too late");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.InvalidStatusTransition", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_MissingReason_ShouldReturnValidationFailure()
    {
        var command = new RejectMerchantCommand(Guid.NewGuid(), "");
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Reason", "A rejection reason is required.") }));

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Reason", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_MerchantNotFound_ShouldFail()
    {
        var command = new RejectMerchantCommand(Guid.NewGuid(), "reason");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((MerchantEntity?)null);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.MerchantNotFound", result.Errors[0].Code);
    }

    private MerchantEntity CreatePendingMerchant() =>
        new(Guid.NewGuid(), new BusinessName("Test"), new MerchantEmail("test@test.com"));

    private void SetupValidatorSuccess(RejectMerchantCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
}