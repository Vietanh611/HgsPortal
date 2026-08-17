using Core.Interfaces;
using Data.DbContexts;
using Domain.Entities.System;
using Hgs.Share.Requests.CriticalLogs;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.CriticalLogs;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.Operations;

public class CriticalLogService : ICriticalLogService
{
    private readonly HgsDbContext _dbContext;

    public CriticalLogService(HgsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResponse<CriticalLogsGetAllResponse>> GetFilteredAsync(
        CriticalLogsFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        // Clamp phân trang (chống DoS qua [FromQuery]) — giữ cùng convention AuditLogService.
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize < 1 ? 20 : request.PageSize, 1, 200);

        var query = ApplyFilters(_dbContext.CriticalLogs.AsNoTracking().AsQueryable(), request);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.TimeStamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CriticalLogsGetAllResponse
            {
                Id = x.Id,
                Message = x.Message,
                MessageTemplate = x.MessageTemplate,
                Level = x.Level,
                TimeStamp = x.TimeStamp,
                Exception = x.Exception,
                Properties = x.Properties,
                RequestId = x.RequestId,
                User = x.User,
                Path = x.Path,
                Method = x.Method
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<CriticalLogsGetAllResponse>
        {
            Items = items,
            TotalCount = total,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)total / pageSize)
        };
    }

    public async Task<CriticalLogsGetAllResponse?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CriticalLogs
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CriticalLogsGetAllResponse
            {
                Id = x.Id,
                Message = x.Message,
                MessageTemplate = x.MessageTemplate,
                Level = x.Level,
                TimeStamp = x.TimeStamp,
                Exception = x.Exception,
                Properties = x.Properties,
                RequestId = x.RequestId,
                User = x.User,
                Path = x.Path,
                Method = x.Method
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<CriticalLogs> ApplyFilters(IQueryable<CriticalLogs> query, CriticalLogsFilterRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Level))
            query = query.Where(x => x.Level == request.Level);

        if (request.FromDate.HasValue)
            query = query.Where(x => x.TimeStamp >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(x => x.TimeStamp <= request.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(x =>
                (x.Message != null && x.Message.Contains(keyword)) ||
                (x.MessageTemplate != null && x.MessageTemplate.Contains(keyword)) ||
                (x.Exception != null && x.Exception.Contains(keyword)) ||
                (x.Properties != null && x.Properties.Contains(keyword)));
        }

        return query;
    }
}
