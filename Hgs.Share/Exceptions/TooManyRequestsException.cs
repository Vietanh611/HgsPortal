namespace Hgs.Share.Exceptions;

public class TooManyRequestsException : BaseException
{
    public TooManyRequestsException(string message) : base(message, 429, "TOO_MANY_REQUESTS")
    {
    }

    public TooManyRequestsException(string message, Exception innerException) 
        : base(message, 429, innerException, "TOO_MANY_REQUESTS")
    {
    }
}
