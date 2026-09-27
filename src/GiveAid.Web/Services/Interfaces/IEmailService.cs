namespace GiveAid.Web.Services.Interfaces;

public interface IEmailService
{
    Task<(bool Success, string? ErrorMessage)> SendEmailAsync(string toEmail, string subject, string htmlBody);
    Task<(bool Success, string? ErrorMessage)> SendOtpEmailAsync(string toEmail, string recipientName, string otpCode, int expiryMinutes = 1);
}
