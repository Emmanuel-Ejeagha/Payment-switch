using BuildingBlocks.Shared.Security;

namespace BuildingBlocks.Shared.Tests;

public class PasswordPolicyTests
{
    [Fact]
    public void IsValid_StrongPassword_True()
    {
        Assert.True(PasswordPolicy.IsValid("Morningcoffee1"));
        Assert.True(PasswordPolicy.IsValid("correct-horse-9-battery"));
    }

    [Fact]
    public void IsValid_TooShort_False()
    {
        Assert.False(PasswordPolicy.IsValid("Abc1234567"));
    }

    [Fact]
    public void IsValid_JustAtMinimum_True()
    {
        Assert.True(PasswordPolicy.IsValid(new string('a', PasswordPolicy.MinLength - 2) + "A1"));
    }

    [Fact]
    public void IsValid_MissingDigit_False()
    {
        Assert.False(PasswordPolicy.IsValid("morningcoffee"));
    }

    [Fact]
    public void IsValid_MissingLetter_False()
    {
        Assert.False(PasswordPolicy.IsValid("123456789012"));
    }

    [Fact]
    public void IsValid_NoUppercaseOrSymbol_False()
    {
        Assert.False(PasswordPolicy.IsValid("morningcoffee1"));
    }

    [Theory]
    [InlineData("password1234")]
    [InlineData("Password1234")]
    [InlineData("PASSWORD1234")]
    [InlineData("qwerty123")]
    [InlineData("letmein123")]
    public void IsValid_CommonPassword_False(string password)
    {
        Assert.False(PasswordPolicy.IsValid(password));
        Assert.True(PasswordPolicy.IsCommonPassword(password));
    }

    [Fact]
    public void IsCommonPassword_LongerUniquePassphrase_False()
    {
        Assert.False(PasswordPolicy.IsCommonPassword("Test123456!"));
        Assert.False(PasswordPolicy.IsCommonPassword("Morningcoffee1"));
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