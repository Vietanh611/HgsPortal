using Hgs.Share.Responses.ApiResponses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Serilog.Context;
using System.Text.Json;

namespace API.Authorization;

public class MenuAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();
    private readonly ILogger<MenuAuthorizationResultHandler> _logger;

    public MenuAuthorizationResultHandler(ILogger<MenuAuthorizationResultHandler> logger)
    {
        _logger = logger;
    }

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
