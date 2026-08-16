namespace Prodhab.Api.Exceptions;

// Autenticado pero sin permiso sobre el recurso. El handler global la traduce a 403.
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
