namespace Hgs.Share.Exceptions;

public class ValidationException : BaseException
{
    public Dictionary<string, string[]> Errors { get; }

    public ValidationException(string message) : base(message, 400, "VALIDATION_ERROR")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(string message, Dictionary<string, string[]> errors) 
        : base(message, 400, "VALIDATION_ERROR")
    {
        Errors = errors;
    }

    public ValidationException(string message, Exception innerException) 
        : base(message, 400, innerException, "VALIDATION_ERROR")
    {
        Errors = new Dictionary<string, string[]>();
    }
}
