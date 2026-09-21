using GiveAid.Web.Data;
using GiveAid.Web.Models.Entities;
using GiveAid.Web.Models.Enums;
using GiveAid.Web.Models.ViewModels.Queries;
using GiveAid.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Web.Services.Implementations;

public class QueryService : IQueryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QueryService> _logger;

    public QueryService(ApplicationDbContext context, ILogger<QueryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Query> SubmitQueryAsync(QueryCreateViewModel model, int? userId)
    {
        var query = new Query
        {
            UserId = userId,
            Name = model.Name,
            Email = model.Email,
            Subject = model.Subject,
            Message = model.Message,
            Status = QueryStatuses.Open,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Queries.AddAsync(query);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Query #{Id} created by {Email}", query.Id, query.Email);
        return query;
    }

    public async Task<List<Query>> GetUserQueriesAsync(int userId)
    {
        return await _context.Queries
            .Include(q => q.Replies)
                .ThenInclude(r => r.AdminUser)
            .Where(q => q.UserId == userId)
            .OrderByDescending(q => q.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Query>> GetAllQueriesAsync(string? status = null)
    {
        var queryable = _context.Queries
            .Include(q => q.Replies)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            queryable = queryable.Where(q => q.Status == status);
        }

        return await queryable.OrderByDescending(q => q.CreatedAt).ToListAsync();
    }

    public async Task<Query?> GetQueryByIdAsync(int id)
    {
        return await _context.Queries
            .Include(q => q.Replies)
                .ThenInclude(r => r.AdminUser)
            .Include(q => q.User)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task<bool> ReplyToQueryAsync(int queryId, int adminUserId, string replyMessage)
    {
        var query = await _context.Queries.FirstOrDefaultAsync(q => q.Id == queryId);
        if (query == null) return false;

        var reply = new QueryReply
        {
            QueryId = queryId,
            AdminUserId = adminUserId,
            Message = replyMessage,
            RepliedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _context.QueryReplies.AddAsync(reply);
        query.Status = QueryStatuses.Answered;
        query.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Admin {AdminId} replied to query #{QueryId}", adminUserId, queryId);
        return true;
    }
}
