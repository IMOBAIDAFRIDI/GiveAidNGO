using System.ComponentModel.DataAnnotations;
using GiveAid.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GiveAid.Web.Models.ViewModels.Donations;

public class DonationCreateViewModel
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
    [Display(Name = "Your Full Name")]
    public string DonorName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required for the donation receipt.")]
    [EmailAddress]
    [Display(Name = "Email Address")]
    public string DonorEmail { get; set; } = string.Empty;

    // Card Payment Fields (Processed in-memory only, NEVER stored in DB)
    [Required(ErrorMessage = "Card number is required.")]
    [CreditCard(ErrorMessage = "Please enter a valid card number.")]
    [Display(Name = "Card Number")]
    public string CardNumber { get; set; } = "4242424242424242";

    [Required(ErrorMessage = "Expiry is required.")]
    [RegularExpression(@"^(0[1-9]|1[0-2])\/?([0-9]{2})$", ErrorMessage = "Format must be MM/YY.")]
    [Display(Name = "Expiration (MM/YY)")]
    public string ExpirationDate { get; set; } = "12/28";

    [Required(ErrorMessage = "Security code is required.")]
    [RegularExpression(@"^[0-9]{3,4}$", ErrorMessage = "CVV must be 3 or 4 digits.")]
    [Display(Name = "CVV")]
    public string Cvv { get; set; } = "123";

    // SelectList populated by Controller
    public List<SelectListItem> AvailableCauses { get; set; } = new();
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
