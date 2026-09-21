using GiveAid.Web.Models.Entities;
using GiveAid.Web.Models.ViewModels.Donations;

namespace GiveAid.Web.Services.Interfaces;

public interface IDonationService
{
    Task<DonationResultViewModel> ProcessDemoDonationAsync(DonationCreateViewModel model, int? userId);
    Task<DonationResultViewModel?> GetDonationByReferenceAsync(string referenceNo);
    Task<List<Donation>> GetUserDonationsAsync(int userId);
    Task<List<Donation>> GetAllDonationsAsync(string? status = null, int? causeId = null);
}
