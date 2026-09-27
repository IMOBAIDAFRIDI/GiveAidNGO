using System.ComponentModel.DataAnnotations;

namespace GiveAid.Web.Models.ViewModels.Account;

public class VerifyEmailOtpViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the 6-digit verification code.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be exactly 6 digits.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be numeric only.")]
    [Display(Name = "Verification Code")]
    public string OtpCode { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }

    public int RemainingSeconds { get; set; } = 60;
    public int ResendAttemptsUsed { get; set; } = 0;
    public bool IsLocked { get; set; } = false;
    public int LockRemainingMinutes { get; set; } = 0;
}
