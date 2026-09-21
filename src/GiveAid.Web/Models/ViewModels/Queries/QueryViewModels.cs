using System.ComponentModel.DataAnnotations;
using GiveAid.Web.Models.Entities;

namespace GiveAid.Web.Models.ViewModels.Queries;

public class QueryCreateViewModel
{
    [Required(ErrorMessage = "Your name is required.")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(250)]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Message is required.")]
    [StringLength(3000, MinimumLength = 10, ErrorMessage = "Message must be between 10 and 3000 characters.")]
    public string Message { get; set; } = string.Empty;
}

public class QueryReplyViewModel
{
    public int QueryId { get; set; }
    public Query Query { get; set; } = null!;

    [Required(ErrorMessage = "Reply message cannot be empty.")]
    [StringLength(4000)]
    public string ReplyMessage { get; set; } = string.Empty;
}

public class InvitationCreateViewModel
{
    [Required(ErrorMessage = "Recipient email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid recipient email address.")]
    [Display(Name = "Friend's Email Address")]
    public string RecipientEmail { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Personal note cannot exceed 500 characters.")]
    [Display(Name = "Personal Note (Optional)")]
    public string? Message { get; set; }
}
