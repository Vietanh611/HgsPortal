namespace Hgs.Share.Exceptions;

public class ForbiddenException : BaseException
{
    public ForbiddenException(string message) : base(message, 403, "FORBIDDEN")
    {
    }

    public ForbiddenException(string message, Exception innerException) 
        : base(message, 403, innerException, "FORBIDDEN")
    {
    }
}
