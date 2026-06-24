namespace BuildingBlocks.Shared.Exceptions;

public static class StringExtensions
{
    public static bool IsEmpty(this string? value) => string.IsNullOrEmpty(value);

    public static bool IsNullOrWhiteSpace(this string? value) => string.IsNullOrWhiteSpace(value);
}
