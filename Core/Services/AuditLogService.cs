using Core.Interfaces;
using Data.DbContexts;
using Domain.Entities.Identity;
using Hgs.Share.Attributes;
using Hgs.Share.Responses.AuditLogs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Core.Services
{
    public class AuditLogService : IAuditLogService
    {
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

