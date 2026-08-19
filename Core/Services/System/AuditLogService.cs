using Core.Interfaces.Operations;
using Data.DbContexts;
using Domain.Entities.Identity;
using Hgs.Share.Attributes;
using Hgs.Share.Requests.Audit;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.AuditLogs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Core.Services.System
{
    public class AuditLogService : IAuditLogService
    {
        private const int MaxExportRows = 50_000;
        private readonly HgsDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogService(HgsDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public void Log(string action, string entityName, int? entityId, object? oldValue, object? newValue)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var log = new AuditLogs
            {
                UserId = GetCurrentUserId(httpContext),
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                OldValue = oldValue is null ? null : JsonSerializer.Serialize(oldValue, JsonOptions),
                NewValue = newValue is null ? null : JsonSerializer.Serialize(newValue, JsonOptions),
                IpAddress = GetClientIp(httpContext),
                CorrelationId = null, // không dùng, giữ NULL
                CreatedAt = DateTime.UtcNow
            };

            // Chỉ Add — KHÔNG SaveChanges ở đây.
            // Nơi gọi (business code) sẽ SaveChangesAsync() 1 lần duy nhất,
            // gộp chung với thay đổi nghiệp vụ trong cùng transaction.
            _dbContext.AuditLogs.Add(log);
        }

        public async Task<(IEnumerable<AuditLogsGetAllResponse> Items, int TotalCount)> GetAllAsync(
            int pageNumber = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext.AuditLogs
                .Include(x => x.User)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AuditLogsGetAllResponse
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    Username = x.User != null ? x.User.Username : null,
                    Action = x.Action,
                    EntityName = x.EntityName,
                    EntityId = x.EntityId,
                    OldValue = x.OldValue,
                    NewValue = x.NewValue,
                    IpAddress = x.IpAddress,
                    CorrelationId = x.CorrelationId,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task LogSecurityEventAsync(
            string action,
            string eventCategory,
            bool success,
            string severity,
            int? userId = null,
            int? targetUserId = null,
            string? username = null,
            string? entityName = null,
            int? entityId = null,
            string? detail = null,
            object? oldValue = null,
            object? newValue = null,
            CancellationToken cancellationToken = default)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var log = new AuditLogs
            {
                UserId = userId ?? GetCurrentUserId(httpContext),
                TargetUserId = targetUserId,
                Username = username,
                EventCategory = eventCategory,
                Action = action,
                EntityName = entityName ?? string.Empty,
                EntityId = entityId,
                OldValue = oldValue is null ? null : JsonSerializer.Serialize(oldValue, JsonOptions),
                NewValue = newValue is null ? detail : JsonSerializer.Serialize(newValue, JsonOptions),
                Success = success,
                Severity = severity,
                IpAddress = GetClientIp(httpContext),
                CorrelationId = null,
                CreatedAt = DateTime.UtcNow
            };

            // TỰ SaveChangesAsync — khác Log (Add-only). Các sự kiện bảo mật rơi vào nhánh fail
            // mà service throw trước khi có SaveChangesAsync nghiệp vụ; nếu chỉ Add thì dòng log
            // brute-force quan trọng nhất sẽ bị mất âm thầm.
            _dbContext.AuditLogs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<PagedResponse<AuditLogsGetAllResponse>> GetFilteredAsync(
            AuditLogsFilterRequest request,
            CancellationToken cancellationToken = default)
        {
            // Clamp phân trang (chống DoS qua [FromQuery]) — mục 2.1 spec
            var pageNumber = Math.Max(1, request.PageNumber);
            var pageSize = Math.Clamp(request.PageSize < 1 ? 20 : request.PageSize, 1, 200);

            var query = ApplyFilters(_dbContext.AuditLogs.AsNoTracking().AsQueryable(), request);

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuditLogsGetAllResponse
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    TargetUserId = a.TargetUserId,
                    // Ưu tiên cột Username (denormalize), fallback nav User.Username — không 2 nguồn mơ hồ
                    Username = a.Username != null ? a.Username : a.User != null ? a.User.Username : null,
                    TargetUsername = a.TargetUser != null ? a.TargetUser.Username : null,
                    EventCategory = a.EventCategory,
                    Success = a.Success,
                    Severity = a.Severity,
                    Action = a.Action,
                    EntityName = a.EntityName,
                    EntityId = a.EntityId,
                    OldValue = a.OldValue,
                    NewValue = a.NewValue,
                    IpAddress = a.IpAddress,
                    CorrelationId = a.CorrelationId,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new PagedResponse<AuditLogsGetAllResponse>
            {
                Items = items,
                TotalCount = total,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)total / pageSize)
            };
        }

        public async Task<long> CountAsync(
            AuditLogsFilterRequest request,
            CancellationToken cancellationToken = default)
        {
            var query = ApplyFilters(_dbContext.AuditLogs.AsNoTracking().AsQueryable(), request);
            return await query.LongCountAsync(cancellationToken);
        }

        public async Task<List<AuditLogs>> GetAllFilteredAsync(
            AuditLogsFilterRequest request,
            CancellationToken cancellationToken = default)
        {
            // Không Skip/Take — dùng cho export; cap cứng 50.000 phòng race (dữ liệu tăng
            // giữa lúc count ở export service và lúc query thật).
            var query = ApplyFilters(_dbContext.AuditLogs
                    .Include(a => a.User)
                    .Include(a => a.TargetUser)
                    .AsNoTracking()
                    .AsQueryable(), request);

            return await query
                .OrderByDescending(a => a.CreatedAt)
                .Take(MaxExportRows)
                .ToListAsync(cancellationToken);
        }

        private static IQueryable<AuditLogs> ApplyFilters(IQueryable<AuditLogs> query, AuditLogsFilterRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.EntityName))
                query = query.Where(a => a.EntityName == request.EntityName);

            if (request.EntityId.HasValue)
                query = query.Where(a => a.EntityId == request.EntityId);

            if (request.UserId.HasValue)
                query = query.Where(a => a.UserId == request.UserId);

            if (request.TargetUserId.HasValue)
                query = query.Where(a => a.TargetUserId == request.TargetUserId);

            if (!string.IsNullOrWhiteSpace(request.EventCategory))
                query = query.Where(a => a.EventCategory == request.EventCategory);

            if (!string.IsNullOrWhiteSpace(request.Action))
                query = query.Where(a => a.Action == request.Action);

            if (request.Success.HasValue)
                query = query.Where(a => a.Success == request.Success);

            if (!string.IsNullOrWhiteSpace(request.Severity))
                query = query.Where(a => a.Severity == request.Severity);

            if (request.FromDate.HasValue)
                query = query.Where(a => a.CreatedAt >= request.FromDate);

            if (request.ToDate.HasValue)
                query = query.Where(a => a.CreatedAt <= request.ToDate);

            if (!string.IsNullOrWhiteSpace(request.Keyword))
                query = query.Where(a =>
                    a.Username != null && a.Username.Contains(request.Keyword) ||
                    a.IpAddress != null && a.IpAddress.Contains(request.Keyword) ||
                    a.EntityId != null && a.EntityId.ToString()!.Contains(request.Keyword));

            return query;
        }

        private static int? GetCurrentUserId(HttpContext? httpContext)
        {
            // Đổi ClaimTypes.NameIdentifier thành tên claim thực tế
            // đang dùng khi issue JWT (VD: "uid", "sub"...)
            var claim = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var userId) ? userId : null;
        }

        private static string? GetClientIp(HttpContext? httpContext)
        {
            return httpContext?.Connection?.RemoteIpAddress?.ToString();
        }
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,   // không escape Unicode
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { IgnoreAuditIgnoreProperties }
            }
        };
        private static void IgnoreAuditIgnoreProperties(JsonTypeInfo typeInfo)
        {
            foreach (var property in typeInfo.Properties)
            {
                if (property.AttributeProvider?.IsDefined(typeof(AuditIgnoreAttribute), inherit: true) == true)
                {
                    property.ShouldSerialize = (_, _) => false;
                }
            }
        }
    }
}

