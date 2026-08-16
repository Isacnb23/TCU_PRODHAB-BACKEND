namespace Prodhab.Api.Exceptions;

// Falta autenticación o las credenciales no son válidas. El handler global la traduce a 401.
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}
