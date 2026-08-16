namespace Prodhab.Api.Exceptions;

// El recurso solicitado no existe. El handler global la traduce a 404.
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
