using Hgs.Share.Exceptions;
using Hgs.Share.Responses.ApiResponses;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _env;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        IHostEnvironment env,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _env = env;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "[{RequestId}] {Method} {Path} => Request aborted by client",
                    context.TraceIdentifier,
                    context.Request.Method,
                    context.Request.Path);
                return;
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var requestId = context.TraceIdentifier;
        var method = context.Request.Method;
        var path = context.Request.Path;

        context.Response.ContentType = "application/json";

        int statusCode;
        string message;
        object? data = null;

        switch (exception)
        {
            case NotFoundException ex:
                statusCode = ex.StatusCode;
                message = ex.Message;
                break;

            case BadRequestException ex:
                statusCode = ex.StatusCode;
                message = ex.Message;
                break;

            case ValidationException ex:
                statusCode = ex.StatusCode;
                message = ex.Message;
                data = ex.Errors;
                break;

            case UnauthorizedException ex:
                statusCode = ex.StatusCode;
                message = ex.Message;
                break;

            case ForbiddenException ex:
                statusCode = ex.StatusCode;
                message = ex.Message;
                break;

            case ConflictException ex:
                statusCode = ex.StatusCode;
                message = ex.Message;
                break;

            case TooManyRequestsException ex:
                statusCode = ex.StatusCode;
                message = ex.Message;
                break;

            case BusinessRuleException ex:
                statusCode = ex.StatusCode;
                message = ex.Message;
                break;

            case DbUpdateException ex when ex.GetBaseException() is SqlException sqlEx:
                statusCode = StatusCodes.Status500InternalServerError;
                message = $"A database error occurred. {sqlEx.Message}";
                break;

            case SqlException sqlEx:
                statusCode = StatusCodes.Status500InternalServerError;
                message = $"A database error occurred. {sqlEx.Message}";
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                message = $"An unexpected error occurred.{exception.Message}. Please try again later.";
                break;
        }

        context.Response.StatusCode = statusCode;

        // Log đúng 1 lần
        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "[{RequestId}] {Method} {Path} => {StatusCode} - {Message}",
                requestId,
                method,
                path,
                statusCode,
                exception.Message);
        }
        else
        {
            _logger.LogWarning(
                "[{RequestId}] {Method} {Path} => {StatusCode} - {Message}",
                requestId,
                method,
                path,
                statusCode,
                exception.Message);
        }

        var response = ApiResponse<object>.FailResponse(message, statusCode);

        response.Data = data;

        if (_env.IsDevelopment())
        {
            response.Data = new
            {
                RequestId = requestId,
                Exception = exception.GetType().Name,
                Message = exception.Message,
                StackTrace = exception.StackTrace,
                ValidationErrors = data
            };
        }

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }
}