using GiveAid.Web.Models.Entities;
using GiveAid.Web.Models.ViewModels.Queries;

namespace GiveAid.Web.Services.Interfaces;

public interface IInvitationService
{
    Task<Invitation> SendInvitationAsync(InvitationCreateViewModel model, int inviterUserId);
    Task<List<Invitation>> GetUserInvitationsAsync(int inviterUserId);
    Task<List<Invitation>> GetAllInvitationsAsync();
}
