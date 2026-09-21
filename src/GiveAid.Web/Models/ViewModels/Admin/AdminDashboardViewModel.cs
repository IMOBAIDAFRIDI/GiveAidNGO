using GiveAid.Web.Models.Entities;

namespace GiveAid.Web.Models.ViewModels.Admin;

public class AdminDashboardViewModel
{
    // KPIs (Calculated from SQL Server queries)
    public int TotalMembers { get; set; }
    public decimal TotalDonationsAmount { get; set; }
    public int TotalDonationsCount { get; set; }
    public int ActiveCausesCount { get; set; }
    public int UpcomingProgrammesCount { get; set; }
    public int OpenQueriesCount { get; set; }

    // Recent activity feeds
    public List<Donation> RecentDonations { get; set; } = new();
    public List<ApplicationUser> RecentMembers { get; set; } = new();
    public List<ProgrammeInterest> RecentInterests { get; set; } = new();
    public List<AdminActivityLog> RecentActivityLogs { get; set; } = new();
}
