using System.Text.RegularExpressions;

namespace GiveAid.Web.Services.Payments;

public record PaymentResult(
    bool IsSuccess,
    string GatewayReference,
    string MaskedCard,
    string? FailureReason = null
);

public interface IDemoPaymentGateway
{
    Task<PaymentResult> ProcessPaymentAsync(string cardNumber, string expiry, string cvv, decimal amount);
}

public class DemoPaymentGateway : IDemoPaymentGateway
{
    private static readonly Regex ExpiryFormatRegex = new(@"^(0[1-9]|1[0-2])\/?([0-9]{2}|[0-9]{4})$", RegexOptions.Compiled);

    public Task<PaymentResult> ProcessPaymentAsync(string cardNumber, string expiry, string cvv, decimal amount)
    {
        // Sanitize card to digits only
        var cleanNumber = (cardNumber ?? "").Replace(" ", "").Replace("-", "").Trim();
        var last4 = cleanNumber.Length >= 4 ? cleanNumber[^4..] : "0000";
        var masked = $"**** **** **** {last4}";
        var reference = $"PAY-GW-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        // 1. Validate Card Number (Length and Digits)
        if (string.IsNullOrWhiteSpace(cleanNumber) || !cleanNumber.All(char.IsDigit) || cleanNumber.Length < 13 || cleanNumber.Length > 19)
        {
            return Task.FromResult(new PaymentResult(
                IsSuccess: false,
                GatewayReference: reference,
                MaskedCard: masked,
                FailureReason: "Invalid card number format. Must contain 13 to 19 digits."
            ));
        }

        // 2. Validate Luhn Checksum
        if (!IsValidLuhn(cleanNumber))
        {
            return Task.FromResult(new PaymentResult(
                IsSuccess: false,
                GatewayReference: reference,
                MaskedCard: masked,
                FailureReason: "Invalid card number. Failed checksum verification."
            ));
        }

        // 3. Validate Expiration Date against Current Date
        if (string.IsNullOrWhiteSpace(expiry))
        {
            return Task.FromResult(new PaymentResult(
                IsSuccess: false,
                GatewayReference: reference,
                MaskedCard: masked,
                FailureReason: "Card expiration date is required."
            ));
        }

        var expiryMatch = ExpiryFormatRegex.Match(expiry.Trim());
        if (!expiryMatch.Success)
        {
            return Task.FromResult(new PaymentResult(
                IsSuccess: false,
                GatewayReference: reference,
                MaskedCard: masked,
                FailureReason: "Invalid expiration date format. Format must be MM/YY."
            ));
        }

        var month = int.Parse(expiryMatch.Groups[1].Value);
        var yearShort = int.Parse(expiryMatch.Groups[2].Value);
        var fullYear = yearShort < 100 ? 2000 + yearShort : yearShort;
        var endOfExpiryMonth = new DateTime(fullYear, month, DateTime.DaysInMonth(fullYear, month), 23, 59, 59, DateTimeKind.Utc);

        if (endOfExpiryMonth < DateTime.UtcNow)
        {
            return Task.FromResult(new PaymentResult(
                IsSuccess: false,
                GatewayReference: reference,
                MaskedCard: masked,
                FailureReason: "Card has expired. The expiration date must be in the future."
            ));
        }

        // 4. Validate CVV
        var cleanCvv = (cvv ?? "").Trim();
        if (string.IsNullOrWhiteSpace(cleanCvv) || !cleanCvv.All(char.IsDigit) || (cleanCvv.Length != 3 && cleanCvv.Length != 4))
        {
            return Task.FromResult(new PaymentResult(
                IsSuccess: false,
                GatewayReference: reference,
                MaskedCard: masked,
                FailureReason: "Invalid CVV security code. CVV must be 3 or 4 numeric digits."
            ));
        }

        // 5. Validate Amount
        if (amount < 5.00m)
        {
            return Task.FromResult(new PaymentResult(
                IsSuccess: false,
                GatewayReference: reference,
                MaskedCard: masked,
                FailureReason: "Minimum donation amount is $5.00."
            ));
        }

        // 6. Network decline triggers: if amount is 9999.00 or card ends with 0000, trigger decline
        if (amount == 9999.00m || last4 == "0000")
        {
            return Task.FromResult(new PaymentResult(
                IsSuccess: false,
                GatewayReference: reference,
                MaskedCard: masked,
                FailureReason: "Card was declined by payment network. Please check your card details or contact your bank."
            ));
        }

        return Task.FromResult(new PaymentResult(
            IsSuccess: true,
            GatewayReference: reference,
            MaskedCard: masked
        ));
    }

    private static bool IsValidLuhn(string number)
    {
        int sum = 0;
        bool alternate = false;
        for (int i = number.Length - 1; i >= 0; i--)
        {
            int n = number[i] - '0';
            if (alternate)
            {
                n *= 2;
                if (n > 9) n -= 9;
            }
            sum += n;
            alternate = !alternate;
        }
        return (sum % 10 == 0);
    }
}
