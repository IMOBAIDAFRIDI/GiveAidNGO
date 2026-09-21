using GiveAid.Web.Data;
using GiveAid.Web.Models.Entities;
using GiveAid.Web.Models.Enums;
using GiveAid.Web.Models.ViewModels.Admin;
using GiveAid.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Web.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminDashboardViewModel> GetDashboardMetricsAsync()
    {
        var totalMembers = await _context.Users
            .Where(u => u.Role == SystemRoles.Member)
            .CountAsync();

        var totalDonationsAmount = await _context.Donations
            .Where(d => d.Status == DonationStatuses.Successful)
            .SumAsync(d => (decimal?)d.Amount) ?? 0.00m;

        var totalDonationsCount = await _context.Donations
            .Where(d => d.Status == DonationStatuses.Successful)
            .CountAsync();

        var activeCausesCount = await _context.Causes
            .Where(c => c.IsActive)
            .CountAsync();

        var upcomingProgrammesCount = await _context.Programmes
            .Where(p => p.Status == ProgrammeStatuses.Published && p.StartAt >= DateTime.UtcNow)
            .CountAsync();

        var openQueriesCount = await _context.Queries
            .Where(q => q.Status == QueryStatuses.Open)
            .CountAsync();

        var recentDonations = await _context.Donations
            .Include(d => d.Cause)
            .OrderByDescending(d => d.CreatedAt)
            .Take(6)
            .AsNoTracking()
            .ToListAsync();

        var recentMembers = await _context.Users
            .Where(u => u.Role == SystemRoles.Member)
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .AsNoTracking()
            .ToListAsync();

        var recentInterests = await _context.ProgrammeInterests
            .Include(pi => pi.Programme)
            .Include(pi => pi.User)
            .OrderByDescending(pi => pi.InterestedAt)
            .Take(5)
            .AsNoTracking()
            .ToListAsync();

        var recentActivityLogs = await _context.AdminActivityLogs
            .Include(al => al.AdminUser)
            .OrderByDescending(al => al.CreatedAt)
            .Take(6)
            .AsNoTracking()
            .ToListAsync();

        return new AdminDashboardViewModel
        {
            TotalMembers = totalMembers,
            TotalDonationsAmount = totalDonationsAmount,
            TotalDonationsCount = totalDonationsCount,
            ActiveCausesCount = activeCausesCount,
            UpcomingProgrammesCount = upcomingProgrammesCount,
            OpenQueriesCount = openQueriesCount,
            RecentDonations = recentDonations,
            RecentMembers = recentMembers,
            RecentInterests = recentInterests,
            RecentActivityLogs = recentActivityLogs
        };
    }
}
