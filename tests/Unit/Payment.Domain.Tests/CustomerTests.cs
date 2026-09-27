using Payment.Domain.Entities;

namespace Payment.Domain.Tests;

public class CustomerTests
{
    private readonly Guid _merchantId = Guid.NewGuid();

    private Customer CreateCustomer(string email = "Jane@Example.com")
        => new(Guid.NewGuid(), _merchantId, Customer.NewCode(), email, "Jane Doe", "+2348012345678");

    [Fact]
    public void Constructor_ShouldNormalizeEmailAndSetDefaults()
    {
        var customer = CreateCustomer();

        Assert.Equal("jane@example.com", customer.Email);
        Assert.Equal(_merchantId, customer.MerchantId);
        Assert.False(customer.Deleted);
        Assert.Null(customer.UpdatedAt);
        Assert.NotEqual(default, customer.CreatedAt);
    }

    [Fact]
    public void NewCode_ShouldUseCusPrefix()
    {
        Assert.StartsWith("cus_", Customer.NewCode());
        Assert.NotEqual(Customer.NewCode(), Customer.NewCode());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_ShouldRejectMissingEmail(string? email)
    {
        Assert.Throws<ArgumentException>(() =>
            new Customer(Guid.NewGuid(), _merchantId, Customer.NewCode(), email!));
    }

    [Fact]
    public void Constructor_ShouldRejectEmptyMerchantId()
    {
        Assert.Throws<ArgumentException>(() =>
            new Customer(Guid.NewGuid(), Guid.Empty, Customer.NewCode(), "jane@example.com"));
    }

    [Fact]
    public void Update_ShouldOnlyChangeSuppliedFields()
    {
        var customer = CreateCustomer();

        customer.Update(email: null, name: "Jane Smith", phone: null, description: null);

        Assert.Equal("Jane Smith", customer.Name);
        Assert.Equal("jane@example.com", customer.Email);
        Assert.Equal("+2348012345678", customer.Phone);
        Assert.NotNull(customer.UpdatedAt);
    }

    [Fact]
    public void Update_ShouldNormalizeNewEmail()
    {
        var customer = CreateCustomer();

        customer.Update("NEW@Example.COM", null, null, null);

        Assert.Equal("new@example.com", customer.Email);
    }

    [Fact]
    public void Update_ShouldThrowWhenCustomerDeleted()
    {
        var customer = CreateCustomer();
        customer.MarkDeleted();

        Assert.Throws<InvalidOperationException>(() => customer.Update(null, "Jane Smith", null, null));
    }

    [Fact]
    public void MarkDeleted_ShouldBeIdempotent()
    {
        var customer = CreateCustomer();

        customer.MarkDeleted();
        var firstDeletedAt = customer.UpdatedAt;
        customer.MarkDeleted();

        Assert.True(customer.Deleted);
        Assert.Equal(firstDeletedAt, customer.UpdatedAt);
    }
}
