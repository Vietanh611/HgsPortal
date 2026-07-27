using Hgs.Share.Exceptions;
using Hgs.Share.Responses.ApiResponses;
using Microsoft.Data.SqlClient;
using Serilog;
using System.Net;
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ApiResponse<object>();

        switch (exception)
        {
            case NotFoundException notFoundEx:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse = ApiResponse<object>.FailResponse(notFoundEx.Message, notFoundEx.StatusCode);
                Log.Information("NotFoundException: {Message}", notFoundEx.Message);
                break;

            case UnauthorizedException unauthorizedEx:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse = ApiResponse<object>.FailResponse(unauthorizedEx.Message, unauthorizedEx.StatusCode);
                Log.Information("UnauthorizedException: {Message}", unauthorizedEx.Message);
                break;

            case ForbiddenException forbiddenEx:
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                errorResponse = ApiResponse<object>.FailResponse(forbiddenEx.Message, forbiddenEx.StatusCode);
                Log.Information("ForbiddenException: {Message}", forbiddenEx.Message);
                break;

            case BadRequestException badRequestEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = ApiResponse<object>.FailResponse(badRequestEx.Message, badRequestEx.StatusCode);
                Log.Information("BadRequestException: {Message}", badRequestEx.Message);
                break;

            case ValidationException validationEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = ApiResponse<object>.FailResponse(validationEx.Message, validationEx.StatusCode);
                if (validationEx.Errors.Count > 0)
                {
                    errorResponse.Data = validationEx.Errors;
                }
                Log.Information("ValidationException: {Message} - Errors: {Errors}", 
                    validationEx.Message, JsonSerializer.Serialize(validationEx.Errors));
                break;

            case ConflictException conflictEx:
                response.StatusCode = (int)HttpStatusCode.Conflict;
                errorResponse = ApiResponse<object>.FailResponse(conflictEx.Message, conflictEx.StatusCode);
                Log.Information("ConflictException: {Message}", conflictEx.Message);
                break;

            case TooManyRequestsException tooManyRequestsEx:
                response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                errorResponse = ApiResponse<object>.FailResponse(tooManyRequestsEx.Message, tooManyRequestsEx.StatusCode);
                Log.Information("TooManyRequestsException: {Message}", tooManyRequestsEx.Message);
                break;

            case BusinessRuleException businessRuleEx:
                response.StatusCode = 422; // Unprocessable Entity
                errorResponse = ApiResponse<object>.FailResponse(businessRuleEx.Message, businessRuleEx.StatusCode);
                Log.Information("BusinessRuleException: {Message}", businessRuleEx.Message);
                break;

            case SqlException sqlEx:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse = ApiResponse<object>.FailResponse("A database error occurred. Please try again later.", 500);
                Log.Error(sqlEx, "SqlException: {Message} - Error Number: {ErrorNumber}", 
                    sqlEx.Message, sqlEx.Number);
                break;

            case NullReferenceException nullRefEx:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse = ApiResponse<object>.FailResponse("An internal server error occurred. Please try again later.", 500);
                Log.Error(nullRefEx, "NullReferenceException: {Message}", nullRefEx.Message);
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse = ApiResponse<object>.FailResponse("An unexpected error occurred. Please try again later.", 500);
                Log.Error(exception, "Unhandled Exception: {Type} - {Message}", 
                    exception.GetType().Name, exception.Message);
                break;
        }

        // Include detailed error information in development mode
        if (_env.IsDevelopment())
        {
            var errorDetails = new
            {
                Type = exception.GetType().Name,
                Message = exception.Message,
                StackTrace = exception.StackTrace,
                InnerException = exception.InnerException?.Message
            };

            errorResponse.Data = errorDetails;
        }

        var result = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(result);
    }
}
