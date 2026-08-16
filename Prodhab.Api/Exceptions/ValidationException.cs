namespace Prodhab.Api.Exceptions;

// Entrada inválida del cliente (formato, rango, archivo no permitido).
// El handler global la traduce a 400.
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }
}
