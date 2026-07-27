namespace Hgs.Share.Exceptions;

public class BusinessRuleException : BaseException
{
    public BusinessRuleException(string message) : base(message, 422, "BUSINESS_RULE_VIOLATION")
    {
    }

    public BusinessRuleException(string message, Exception innerException) 
        : base(message, 422, innerException, "BUSINESS_RULE_VIOLATION")
    {
    }
}
