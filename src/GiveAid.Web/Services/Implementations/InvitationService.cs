using System.Security.Cryptography;
using GiveAid.Web.Data;
using GiveAid.Web.Models.Entities;
using GiveAid.Web.Models.Enums;
using GiveAid.Web.Models.ViewModels.Queries;
using GiveAid.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Web.Services.Implementations;

public class InvitationService : IInvitationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InvitationService> _logger;

    public InvitationService(ApplicationDbContext context, ILogger<InvitationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Invitation> SendInvitationAsync(InvitationCreateViewModel model, int inviterUserId)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToHexString(tokenBytes).ToLower();

        var invitation = new Invitation
        {
            InviterUserId = inviterUserId,
            RecipientEmail = model.RecipientEmail,
            Token = token,
            Message = model.Message,
            Status = InvitationStatuses.Sent,
            SentAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(14),
            CreatedAt = DateTime.UtcNow
        };

        await _context.Invitations.AddAsync(invitation);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invitation sent by User #{UserId} to {Email}", inviterUserId, model.RecipientEmail);
        return invitation;
    }

    public async Task<List<Invitation>> GetUserInvitationsAsync(int inviterUserId)
    {
        return await _context.Invitations
            .Where(i => i.InviterUserId == inviterUserId)
            .OrderByDescending(i => i.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Invitation>> GetAllInvitationsAsync()
    {
        return await _context.Invitations
            .Include(i => i.InviterUser)
            .OrderByDescending(i => i.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }
}
