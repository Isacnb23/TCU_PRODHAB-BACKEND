using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Prodhab.Api.Configuracion;

namespace Prodhab.Api.Services;

// Guarda los adjuntos fuera de wwwroot, con nombre GUID y sin exponer rutas al cliente.
public class FileStorageService : IFileStorageService
{
    private readonly AlmacenamientoOptions _opciones;
    private readonly IWebHostEnvironment _entorno;

    public FileStorageService(IOptions<AlmacenamientoOptions> opciones, IWebHostEnvironment entorno)
    {
        _opciones = opciones.Value;
        _entorno = entorno;
    }

    public async Task<ArchivoGuardado> GuardarAsync(IFormFile archivo, string extension, CancellationToken ct)
    {
        var nombreFisico = $"{Guid.NewGuid():N}.{extension}";

        var carpetaBase = CarpetaBaseAbsoluta();
        Directory.CreateDirectory(carpetaBase);

        // El validador ya acotó el tamaño, así que bufferizar en memoria es seguro.
        using var buffer = new MemoryStream();
        await using (var origen = archivo.OpenReadStream())
        {
            await origen.CopyToAsync(buffer, ct);
        }

        var bytes = buffer.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        await File.WriteAllBytesAsync(Path.Combine(carpetaBase, nombreFisico), bytes, ct);

        return new ArchivoGuardado(
            RutaRelativa: $"{RutaBaseNormalizada()}/{nombreFisico}",
            NombreFisico: nombreFisico,
            TamanoBytes: bytes.LongLength,
            HashSha256: hash);
    }

    public Task EliminarAsync(string rutaRelativa, CancellationToken ct)
    {
        var (rutaFisica, existe) = ResolverRutaFisica(rutaRelativa);

        // Si ya no está (borrado manual, limpieza previa) no es un error.
        if (existe)
        {
            File.Delete(rutaFisica);
        }

        return Task.CompletedTask;
    }

    public (string rutaFisica, bool existe) ResolverRutaFisica(string rutaRelativa)
    {
        var carpetaBase = CarpetaBaseAbsoluta();
        var rutaFisica = Path.GetFullPath(Path.Combine(_entorno.ContentRootPath, rutaRelativa));

        // Defensa contra path traversal: si la ruta resuelta se sale de la carpeta base
        // (".." o una ruta absoluta guardada en BD), se trata como inexistente.
        var prefijo = carpetaBase.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!rutaFisica.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
        {
            return (rutaFisica, false);
        }

        return (rutaFisica, File.Exists(rutaFisica));
    }

    private string CarpetaBaseAbsoluta() =>
        Path.GetFullPath(Path.Combine(_entorno.ContentRootPath, _opciones.RutaBase));

    private string RutaBaseNormalizada() =>
        _opciones.RutaBase.Replace('\\', '/').TrimEnd('/');
}
