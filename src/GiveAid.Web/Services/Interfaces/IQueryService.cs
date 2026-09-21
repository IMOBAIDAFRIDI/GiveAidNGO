using GiveAid.Web.Models.Entities;
using GiveAid.Web.Models.ViewModels.Queries;

namespace GiveAid.Web.Services.Interfaces;

public interface IQueryService
{
    Task<Query> SubmitQueryAsync(QueryCreateViewModel model, int? userId);
    Task<List<Query>> GetUserQueriesAsync(int userId);
    Task<List<Query>> GetAllQueriesAsync(string? status = null);
    Task<Query?> GetQueryByIdAsync(int id);
    Task<bool> ReplyToQueryAsync(int queryId, int adminUserId, string replyMessage);
}
