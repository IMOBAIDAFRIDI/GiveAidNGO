using GiveAid.Web.Data;
using GiveAid.Web.Models.Entities;
using GiveAid.Web.Models.Enums;
using GiveAid.Web.Models.ViewModels.Donations;
using GiveAid.Web.Services.Interfaces;
using GiveAid.Web.Services.Payments;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Web.Services.Implementations;

public class DonationService : IDonationService
{
    private readonly ApplicationDbContext _context;
    private readonly IDemoPaymentGateway _paymentGateway;
    private readonly ILogger<DonationService> _logger;

    public DonationService(
        ApplicationDbContext context,
        IDemoPaymentGateway paymentGateway,
        ILogger<DonationService> logger)
    {
        _context = context;
        _paymentGateway = paymentGateway;
        _logger = logger;
    }

    public async Task<DonationResultViewModel> ProcessDemoDonationAsync(DonationCreateViewModel model, int? userId)
    {
        var cause = await _context.Causes.FirstOrDefaultAsync(c => c.Id == model.CauseId && c.IsActive);
        if (cause == null)
        {
            throw new InvalidOperationException("The specified cause does not exist or is no longer active.");
        }

        var referenceNo = $"REF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        var paymentResult = await _paymentGateway.ProcessPaymentAsync(
            model.CardNumber,
            model.ExpirationDate,
            model.Cvv,
            model.Amount);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var donation = new Donation
            {
                UserId = userId,
                CauseId = cause.Id,
                ReferenceNo = referenceNo,
                Amount = model.Amount,
                Currency = model.Currency,
                PaymentMethod = model.PaymentMethod,
                DonorName = model.DonorName,
                DonorEmail = model.DonorEmail,
                MaskedCard = paymentResult.MaskedCard,
                GatewayReference = paymentResult.GatewayReference,
                Status = paymentResult.IsSuccess ? DonationStatuses.Successful : DonationStatuses.Failed,
                FailureReason = paymentResult.FailureReason,
                PaidAt = paymentResult.IsSuccess ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Donations.AddAsync(donation);

            if (paymentResult.IsSuccess)
            {
                cause.RaisedAmount += model.Amount;
                cause.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Processed donation {ReferenceNo} for cause {CauseId} with status {Status}",
                referenceNo, cause.Id, donation.Status);

            return new DonationResultViewModel
            {
                ReferenceNo = donation.ReferenceNo,
                Amount = donation.Amount,
                Currency = donation.Currency,
                CauseName = cause.Name,
                Status = donation.Status,
                DonorName = donation.DonorName,
                DonorEmail = donation.DonorEmail,
                MaskedCard = donation.MaskedCard,
                GatewayReference = donation.GatewayReference,
                FailureReason = donation.FailureReason,
                PaidAt = donation.PaidAt,
                CreatedAt = donation.CreatedAt
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to process donation transaction for reference {ReferenceNo}", referenceNo);
            throw;
        }
    }

    public async Task<DonationResultViewModel?> GetDonationByReferenceAsync(string referenceNo)
    {
        var donation = await _context.Donations
            .Include(d => d.Cause)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.ReferenceNo == referenceNo);

        if (donation == null) return null;

        return new DonationResultViewModel
        {
            ReferenceNo = donation.ReferenceNo,
            Amount = donation.Amount,
            Currency = donation.Currency,
            CauseName = donation.Cause.Name,
            Status = donation.Status,
            DonorName = donation.DonorName,
            DonorEmail = donation.DonorEmail,
            MaskedCard = donation.MaskedCard,
            GatewayReference = donation.GatewayReference,
            FailureReason = donation.FailureReason,
            PaidAt = donation.PaidAt,
            CreatedAt = donation.CreatedAt
        };
    }

    public async Task<List<Donation>> GetUserDonationsAsync(int userId)
    {
        return await _context.Donations
            .Include(d => d.Cause)
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Donation>> GetAllDonationsAsync(string? status = null, int? causeId = null)
    {
        var query = _context.Donations
            .Include(d => d.Cause)
            .Include(d => d.User)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(d => d.Status == status);
        }

        if (causeId.HasValue)
        {
            query = query.Where(d => d.CauseId == causeId.Value);
        }

        return await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
    }
}
