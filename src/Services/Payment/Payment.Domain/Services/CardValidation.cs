namespace Payment.Domain.Services;

public static class CardValidation
{
    public static bool IsValidLuhn(string number)
    {
        if (string.IsNullOrWhiteSpace(number) || number.Any(c => !char.IsDigit(c)))
            return false;

        var sum = 0;
        var alternate = false;
        for (var i = number.Length - 1; i >= 0; i--)
        {
            var digit = number[i] - '0';
            if (alternate)
            {
                digit *= 2;
                if (digit > 9)
                    digit -= 9;
            }
            sum += digit;
            alternate = !alternate;
        }

        return sum % 10 == 0;
    }

    public static bool IsValidExpiry(int month, int year)
    {
        if (month < 1 || month > 12)
            return false;

        var now = DateTime.UtcNow;
        var normalizedYear = year < 100 ? 2000 + year : year;
        return normalizedYear > now.Year || (normalizedYear == now.Year && month >= now.Month);
    }

    public static string DetectBrand(string number)
    {
        if (string.IsNullOrWhiteSpace(number))
            return "Unknown";

        if (number.StartsWith("4"))
            return "Visa";

        if (number.StartsWith("5") && number.Length >= 2 && int.TryParse(number[..2], out var mcPrefix) && mcPrefix >= 51 && mcPrefix <= 55)
            return "Mastercard";

        if (number.StartsWith("34") || number.StartsWith("37"))
            return "Amex";

        if (number.StartsWith("6") && (number.StartsWith("60") || number.StartsWith("62") || number.StartsWith("64") || number.StartsWith("65")))
            return "Discover";

        if (number.StartsWith("50") || number.StartsWith("56") || number.StartsWith("57") || number.StartsWith("58") || number.StartsWith("63") || number.StartsWith("67"))
            return "Maestro";

        return "Unknown";
    }
}
