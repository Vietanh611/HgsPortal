namespace Hgs.Share.Exceptions;

public class ConflictException : BaseException
{
    public ConflictException(string message) : base(message, 409, "CONFLICT")
    {
    }

    public ConflictException(string message, Exception innerException) 
        : base(message, 409, innerException, "CONFLICT")
    {
    }
}
