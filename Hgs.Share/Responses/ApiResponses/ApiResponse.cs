namespace Hgs.Share.Responses.ApiResponses;

public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int? StatusCode { get; set; }
    public string? ErrorCode { get; set; }

    public static ApiResponse SuccessResponse(string? message = null, int? statusCode = null)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            StatusCode = statusCode
        };
    }

    public static ApiResponse FailResponse(string message, int? statusCode = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            StatusCode = statusCode
        };
    }
}

public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, string? message = null, int? statusCode = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            StatusCode = statusCode
        };
    }

    public new static ApiResponse<T> FailResponse(string message, int? statusCode = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            StatusCode = statusCode
        };
    }
}
