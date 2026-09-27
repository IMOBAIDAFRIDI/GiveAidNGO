using System.Net;
using System.Net.Mail;
using GiveAid.Web.Models.Configuration;
using GiveAid.Web.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace GiveAid.Web.Services.Implementations;

public class SmtpEmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<SmtpSettings> options, ILogger<SmtpEmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<(bool Success, string? ErrorMessage)> SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_settings.SenderEmail) || string.IsNullOrWhiteSpace(_settings.Password))
        {
            _logger.LogWarning("[SMTP TEST MODE] Credentials missing. Email to {ToEmail} skipped.", toEmail);
            return (true, null);
        }

        try
        {
            using var client = new SmtpClient(_settings.Server, _settings.Port)
            {
                Credentials = new NetworkCredential(_settings.SenderEmail.Trim(), _settings.Password.Trim()),
                EnableSsl = _settings.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Timeout = 10000 // 10s maximum network timeout
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail.Trim(), _settings.SenderName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail.Trim());

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("SMTP email successfully sent to {ToEmail} with Subject: {Subject}", toEmail, subject);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP error while sending email to {ToEmail}: {Message}", toEmail, ex.Message);
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> SendOtpEmailAsync(string toEmail, string recipientName, string otpCode, int expiryMinutes = 1)
    {
        var subject = $"Your GIVE-AID Verification Code is {otpCode}";

        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{subject}</title>
</head>
<body style=""margin: 0; padding: 0; background-color: #f1f5f9; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #1e293b;"">
    <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #f1f5f9; padding: 30px 10px;"">
        <tr>
            <td align=""center"">
                <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 580px; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 25px rgba(0,0,0,0.06); border: 1px solid #e2e8f0;"">
                    <!-- Brand Header Banner -->
                    <tr>
                        <td align=""center"" style=""background: linear-gradient(135deg, #060911 0%, #0e1526 100%); padding: 30px 20px; border-bottom: 3px solid #10b981;"">
                            <table border=""0"" cellpadding=""0"" cellspacing=""0"">
                                <tr>
                                    <td align=""center"">
                                        <div style=""display: inline-block; background-color: #10b981; color: #ffffff; width: 44px; height: 44px; line-height: 44px; border-radius: 12px; font-size: 22px; font-weight: bold; text-align: center; margin-bottom: 10px;"">
                                            &#10084;
                                        </div>
                                        <div style=""color: #10b981; font-size: 24px; font-weight: 900; letter-spacing: -0.5px;"">GIVE-AID</div>
                                        <div style=""color: #94a3b8; font-size: 11px; text-transform: uppercase; letter-spacing: 2px; margin-top: 2px;"">Humanitarian Non-Profit Org</div>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Body Content -->
                    <tr>
                        <td style=""padding: 35px 30px;"">
                            <h2 style=""margin: 0 0 12px 0; color: #0f172a; font-size: 22px; font-weight: 800;"">Confirm Your Registration</h2>
                            <p style=""margin: 0 0 20px 0; color: #475569; font-size: 15px; line-height: 1.6;"">
                                Hello <strong>{WebUtility.HtmlEncode(recipientName)}</strong>,<br>
                                Thank you for joining the GIVE-AID humanitarian network. To complete your account registration, please enter the one-time verification code below:
                            </p>

                            <!-- OTP Code Box -->
                            <div style=""margin: 30px 0; text-align: center;"">
                                <div style=""display: inline-block; background-color: #f0fdf4; border: 2px dashed #10b981; border-radius: 14px; padding: 18px 36px;"">
                                    <span style=""font-family: 'Courier New', Courier, monospace; font-size: 38px; font-weight: 800; letter-spacing: 10px; color: #065f46; display: block;"">
                                        {otpCode}
                                    </span>
                                </div>
                            </div>

                            <!-- Expiry Alert -->
                            <div style=""background-color: #fef2f2; border-left: 4px solid #ef4444; border-radius: 8px; padding: 12px 16px; margin: 20px 0;"">
                                <p style=""margin: 0; color: #991b1b; font-size: 13px; font-weight: 600;"">
                                    &#9888; Important: This code is valid for <strong>{expiryMinutes} minute{(expiryMinutes > 1 ? "s" : "")} only</strong>.
                                </p>
                                <p style=""margin: 4px 0 0 0; color: #b91c1c; font-size: 12px;"">
                                    If the code expires, you can use the Resend button on the verification screen.
                                </p>
                            </div>

                            <p style=""margin: 25px 0 0 0; color: #64748b; font-size: 13px; line-height: 1.5;"">
                                If you did not initiate this registration request with GIVE-AID, please disregard this email. Your email address remains safe and no account will be activated without this code.
                            </p>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td align=""center"" style=""background-color: #f8fafc; padding: 20px; border-top: 1px solid #e2e8f0; color: #94a3b8; font-size: 12px;"">
                            <p style=""margin: 0 0 6px 0;"">&copy; {DateTime.UtcNow.Year} GIVE-AID Humanitarian Org. All rights reserved.</p>
                            <p style=""margin: 0;"">Islamabad, Pakistan &bull; Verified Non-Profit NGO &bull; support@giveaid.org</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        return await SendEmailAsync(toEmail, subject, htmlBody);
    }
}
