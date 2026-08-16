namespace Prodhab.Api.Services;

public interface IValidadorArchivo
{
    Task<ResultadoValidacionArchivo> ValidarAsync(IFormFile archivo, CancellationToken ct);
}
