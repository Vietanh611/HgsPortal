namespace Hgs.Share.Exceptions;

public class BadRequestException : BaseException
{
    public BadRequestException(string message) : base(message, 400, "BAD_REQUEST")
    {
    }

    public BadRequestException(string message, Exception innerException) 
        : base(message, 400, innerException, "BAD_REQUEST")
    {
    }
}
