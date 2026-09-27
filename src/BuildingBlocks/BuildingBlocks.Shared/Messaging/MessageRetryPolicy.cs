using System.Text;

namespace BuildingBlocks.Shared.Messaging;

public static class MessageRetryPolicy
{
    public const int MaxRetries = 3;
    public const string RetryCountHeader = "x-retry-count";
    public static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);

    public static int GetRetryCount(IDictionary<string, object?>? headers)
    {
        if (headers is null)
            return 0;

        if (!headers.TryGetValue(RetryCountHeader, out var value) || value is null)
            return 0;

        return value switch
        {
            long l => (int)l,
            int i => i,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var n) => n,
            string s when int.TryParse(s, out var n) => n,
            _ => 0
        };
    }

    public static bool ShouldRetry(int retryCount) => retryCount < MaxRetries;
}
