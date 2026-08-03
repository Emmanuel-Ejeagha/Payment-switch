namespace BuildingBlocks.Shared.Security;

public static class DataMasker
{
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return "***";

        var parts = email.Split('@', 2);
        var local = parts[0];
        var domain = parts[1];

        if (local.Length <= 1)
            return $"{local[0]}***@{domain}";

        return $"{local[..1]}***{local[^1]}@{domain}";
    }

    public static string MaskUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "***";

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return "***";

        return $"{uri.Scheme}://{uri.Host}:{uri.Port}{uri.AbsolutePath}";
    }
}
