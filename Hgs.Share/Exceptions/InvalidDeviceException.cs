namespace Hgs.Share.Exceptions;

public class InvalidDeviceException : BaseException
{
    public InvalidDeviceException(string message) : base(message, 401, "INVALID_DEVICE")
    {
    }
}