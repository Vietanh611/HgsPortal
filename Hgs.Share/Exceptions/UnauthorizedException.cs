namespace Hgs.Share.Exceptions;

public class UnauthorizedException : BaseException
{
    public UnauthorizedException(string message, string? errorCode = null) : base(message, 401, errorCode ?? "UNAUTHORIZED")
    {
    }

    public UnauthorizedException(string message, Exception innerException, string? errorCode = null) 
        : base(message, 401, innerException, errorCode ?? "UNAUTHORIZED")
    {
    }
}
