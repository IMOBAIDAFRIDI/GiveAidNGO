namespace GiveAid.Web.Models.Configuration;

public class SmtpSettings
{
    public string Server { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string SenderName { get; set; } = "GIVE-AID Humanitarian Org";
    public string SenderEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}
