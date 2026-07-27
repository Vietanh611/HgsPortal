namespace Hgs.Share.Exceptions;

public class UnauthorizedException : BaseException
{
    public UnauthorizedException(string message) : base(message, 401, "UNAUTHORIZED")
    {
    }

    public UnauthorizedException(string message, Exception innerException) 
        : base(message, 401, innerException, "UNAUTHORIZED")
    {
    }
}
