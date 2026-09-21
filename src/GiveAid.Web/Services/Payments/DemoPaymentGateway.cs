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
    public Task<PaymentResult> ProcessPaymentAsync(string cardNumber, string expiry, string cvv, decimal amount)
    {
        // Sanitize card to last 4 digits only
        var cleanNumber = cardNumber.Replace(" ", "").Replace("-", "");
        var last4 = cleanNumber.Length >= 4 ? cleanNumber[^4..] : "0000";
        var masked = $"**** **** **** {last4}";
        var reference = $"PAY-GW-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        // Verification check: if amount is 9999.00 or card ends with 0000, trigger decline
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
}
