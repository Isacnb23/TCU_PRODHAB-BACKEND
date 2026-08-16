namespace Prodhab.Api.Exceptions;

// Regla de negocio violada. El handler global la traduce a 409.
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}
