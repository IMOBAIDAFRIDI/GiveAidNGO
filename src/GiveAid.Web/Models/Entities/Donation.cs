namespace GiveAid.Web.Models.Entities;

public class Donation
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public virtual ApplicationUser? User { get; set; }

    public int CauseId { get; set; }
    public virtual Cause Cause { get; set; } = null!;

    public string ReferenceNo { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentMethod { get; set; } = "Credit Card";
    public string Status { get; set; } = "Pending";

    // Demo payment safe representations - NEVER store raw card/CVV
    public string? DonorName { get; set; }
    public string? DonorEmail { get; set; }
    public string? MaskedCard { get; set; }
    public string? GatewayReference { get; set; }
    public string? FailureReason { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
