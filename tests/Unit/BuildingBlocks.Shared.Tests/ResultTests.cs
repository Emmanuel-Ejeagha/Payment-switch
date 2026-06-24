using BuildingBlocks.Shared.Results;

namespace BuildingBlocks.Shared.Tests;

public class ResultTests
{
    private readonly Error _testError = new("Test.Error", "Something went wrong");

    [Fact]
    public void Result_Success_ShouldBeSuccessful()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Result_Failure_WithSingleError_ShouldHaveErrors()
    {
        var result = Result.Failure(_testError);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
        Assert.Equal(_testError, result.Errors[0]);
    }

    [Fact]
    public void Result_Failure_WithMultipleErrors_ShouldContainAllErrors()
    {
        var error1 = new Error("E1", "First");
        var error2 = new Error("E2", "Second");

        var result = Result.Failure(error1, error2);

        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(error1, result.Errors);
        Assert.Contains(error2, result.Errors);
    }

    [Fact]
    public void Result_ImplicitConversion_FromError_ShouldBeFailure()
    {
        Result result = _testError;

        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void Result_ImplicitConversion_FromErrorArray_ShouldBeFailure()
    {
        var errors = new[] { _testError, new Error("E3", "Third") };
        Result result = errors;

        Assert.True(result.IsFailure);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void ResultT_Success_ShouldHaveValue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ResultT_Failure_ShouldHaveDefaultValueAndErrors()
    {
        var result = Result<string>.Failure(_testError);

        Assert.True(result.IsFailure);
        Assert.Null(result.Value);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void ResultT_ImplicitConversion_FromValue_ShouldBeSuccess()
    {
        Result<string> result = "hello";

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void ResultT_ImplicitConversion_FromError_ShouldBeFailure()
    {
        Result<decimal> result = _testError;

        Assert.True(result.IsFailure);
        Assert.Equal(default, result.Value);
    }

    [Fact]
    public void Error_Creation_ShouldSetCodeAndMessage()
    {
        var error = new Error("CODE", "Message");

        Assert.Equal("CODE", error.Code);
        Assert.Equal("Message", error.Message);
    }
}