namespace BuildingBlocks.Shared.Exceptions;

public static class Guard
{
    public static void AgainstNull<T>(T? value, string parameterName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(parameterName);
        }
    }

    public static void AgainstNullOrEmpty(string? value, string parameterName)
    {
        AgainstNull(value, parameterName);
        if (value.Length == 0)
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }
    }

    public static void AgainstNullOrWhiteSpace(string? value, string parameterName)
    {
        AgainstNull(value, parameterName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be whitespace.", parameterName);
        }
    }
}
