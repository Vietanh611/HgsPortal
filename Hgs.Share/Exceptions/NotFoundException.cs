namespace Hgs.Share.Exceptions;

public class NotFoundException : BaseException
{
    public NotFoundException(string message) : base(message, 404, "NOT_FOUND")
    {
    }

    public NotFoundException(string message, Exception innerException) 
        : base(message, 404, innerException, "NOT_FOUND")
    {
    }
}
