using Hgs.Share.Responses.ApiResponses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Serilog.Context;
using System.Text.Json;

namespace API.Authorization;

/// <summary>
/// Điều phối kết quả authorization: chặn kết quả Forbidden (đã xác thực nhưng không đủ quyền
/// menu) để ghi log cảnh báo phục vụ giám sát an ninh và trả JSON chuẩn ApiResponse kèm mã 403.
/// Các kết quả khác (chưa xác thực → 401 challenge, thành công) được ủy cho handler mặc định.
/// </summary>
public class MenuAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();
    private readonly ILogger<MenuAuthorizationResultHandler> _logger;

    public MenuAuthorizationResultHandler(ILogger<MenuAuthorizationResultHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Ghi log "Access denied" kèm RequestId/User/Path/Method (để Serilog điền cột phụ của bảng
    /// CriticalLogs) rồi trả 403 với body JSON thống nhất. Đây là điểm quyết định 401 vs 403:
    /// 403 chỉ phát sinh ở nhánh Forbidden; chưa xác thực (401 challenge) do handler mặc định lo.
    /// </summary>
    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            // Ghi nhan truy cap bi tu choi (403) de giam sat bao mat — truoc day
            // handler nay tra 403 ma khong log nen khong co du lieu truy vet.
            var user = context.User.Identity?.Name ?? "Anonymous";
            using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
            using (LogContext.PushProperty("User", user))
            using (LogContext.PushProperty("Path", context.Request.Path))
            using (LogContext.PushProperty("Method", context.Request.Method))
            {
                _logger.LogWarning(
                    "Access denied (403) for user '{User}' to {Method} {Path}.",
                    user, context.Request.Method, context.Request.Path);
            }

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json; charset=utf-8";

            var response = ApiResponse<object>.FailResponse(
                "You do not have permission to access this resource.",
                403);

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
