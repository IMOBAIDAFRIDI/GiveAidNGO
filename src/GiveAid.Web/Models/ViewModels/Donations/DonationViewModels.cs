using System.ComponentModel.DataAnnotations;
using GiveAid.Web.Common.ValidationAttributes;
using GiveAid.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GiveAid.Web.Models.ViewModels.Donations;

public class DonationCreateViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Please select a cause to support.")]
    [Display(Name = "Select Cause")]
    public int CauseId { get; set; }

    [Required(ErrorMessage = "Please enter an amount.")]
    [Range(5.00, 100000.00, ErrorMessage = "Donation amount must be between $5.00 and $100,000.00")]
    [DataType(DataType.Currency)]
    [Display(Name = "Donation Amount ($)")]
    public decimal Amount { get; set; } = 50.00m;

    [Required]
    public string Currency { get; set; } = "USD";

    [Required]
    [Display(Name = "Payment Method")]
    public string PaymentMethod { get; set; } = "Credit Card";

    [Required(ErrorMessage = "Donor name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Donor name must be between 2 and 150 characters.")]
    [Display(Name = "Your Full Name")]
    public string DonorName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required for the donation receipt.")]
    [StrictEmailAddress]
    [Display(Name = "Email Address")]
    public string DonorEmail { get; set; } = string.Empty;

    // Card Payment Fields (Processed in-memory only, NEVER stored in DB)
    [Required(ErrorMessage = "Card number is required.")]
    [CreditCard(ErrorMessage = "Please enter a valid credit or debit card number.")]
    [Display(Name = "Card Number")]
    public string CardNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Expiration date is required.")]
    [FutureCardExpiry]
    [Display(Name = "Expiration (MM/YY)")]
    public string ExpirationDate { get; set; } = string.Empty;

    [Required(ErrorMessage = "Security code is required.")]
    [RegularExpression(@"^[0-9]{3,4}$", ErrorMessage = "CVV must be 3 or 4 digits.")]
    [Display(Name = "CVV")]
    public string Cvv { get; set; } = string.Empty;

    // SelectList populated by Controller
    public List<SelectListItem> AvailableCauses { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(ExpirationDate))
        {
            var trimmed = ExpirationDate.Trim();
            var parts = trimmed.Split(new[] { '/', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && int.TryParse(parts[0], out var month) && int.TryParse(parts[1], out var yearShort))
            {
                if (month < 1 || month > 12)
                {
                    yield return new ValidationResult("Expiration month must be between 01 and 12.", new[] { nameof(ExpirationDate) });
                }
                else
                {
                    var fullYear = yearShort < 100 ? 2000 + yearShort : yearShort;
                    var endOfMonth = new DateTime(fullYear, month, DateTime.DaysInMonth(fullYear, month), 23, 59, 59, DateTimeKind.Utc);
                    if (endOfMonth < DateTime.UtcNow)
                    {
                        yield return new ValidationResult("The card has expired. Please enter a valid future expiration date (MM/YY).", new[] { nameof(ExpirationDate) });
                    }
                }
            }
        }
    }
}

public class DonationResultViewModel
{
    public string ReferenceNo { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string CauseName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? DonorName { get; set; }
    public string? DonorEmail { get; set; }
    public string? MaskedCard { get; set; }
    public string? GatewayReference { get; set; }
    public string? FailureReason { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsSuccess => Status == "Successful";
}
