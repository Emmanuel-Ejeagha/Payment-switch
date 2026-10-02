using BuildingBlocks.Shared.Security;

namespace BuildingBlocks.Shared.Tests;

public class PasswordPolicyTests
{
    [Fact]
    public void IsValid_MinimumLengthWithLetterAndDigit_True()
    {
        Assert.True(PasswordPolicy.IsValid("password1234"));
    }

    [Fact]
    public void IsValid_TooShort_False()
    {
        Assert.False(PasswordPolicy.IsValid("Abc123456"));
    }

    [Fact]
    public void IsValid_JustAtMinimum_True()
    {
        Assert.True(PasswordPolicy.IsValid(new string('a', PasswordPolicy.MinLength - 1) + "1"));
    }

    [Fact]
    public void IsValid_MissingDigit_False()
    {
        Assert.False(PasswordPolicy.IsValid("morningcoffee"));
    }

    [Fact]
    public void IsValid_MissingLetter_False()
    {
        Assert.False(PasswordPolicy.IsValid("1234567890"));
    }

    [Fact]
    public void IsValid_OverMaxLength_False()
    {
        Assert.False(PasswordPolicy.IsValid("a1" + new string('x', PasswordPolicy.MaxLength)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_Blank_AlwaysFalse(string? password)
    {
        Assert.False(PasswordPolicy.IsValid(password));
    }
}