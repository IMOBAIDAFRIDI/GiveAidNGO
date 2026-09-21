using GiveAid.Web.Models.ViewModels.Admin;

namespace GiveAid.Web.Services.Interfaces;

public interface IDashboardService
{
    Task<AdminDashboardViewModel> GetDashboardMetricsAsync();
}
