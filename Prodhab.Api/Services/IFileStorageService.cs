namespace Prodhab.Api.Services;

public interface IFileStorageService
{
    Task<ArchivoGuardado> GuardarAsync(IFormFile archivo, string extension, CancellationToken ct);

    Task EliminarAsync(string rutaRelativa, CancellationToken ct);

    (string rutaFisica, bool existe) ResolverRutaFisica(string rutaRelativa);
}
