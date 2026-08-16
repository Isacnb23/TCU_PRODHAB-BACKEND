using Microsoft.EntityFrameworkCore;
using Prodhab.Api.Data;
using Prodhab.Api.DTOs.Subsanaciones;
using Prodhab.Api.Exceptions;
using Prodhab.Api.Models;

namespace Prodhab.Api.Services;

public class SubsanacionService : ISubsanacionService
{
    private const int PasoMinimo = 1;
    private const int PasoMaximo = 9;

    private const string MimePorDefecto = "application/octet-stream";

    private readonly AppDbContext _db;
    private readonly IValidadorArchivo _validador;
    private readonly IFileStorageService _almacenamiento;
    private readonly ICurrentUserService _currentUser;

    public SubsanacionService(
        AppDbContext db,
        IValidadorArchivo validador,
        IFileStorageService almacenamiento,
        ICurrentUserService currentUser)
    {
        _db = db;
        _validador = validador;
        _almacenamiento = almacenamiento;
        _currentUser = currentUser;
    }

    public async Task<SubsanacionDto> CrearAsync(int expedienteId, CrearSubsanacionForm form, CancellationToken ct)
    {
        if (form.Paso < PasoMinimo || form.Paso > PasoMaximo)
        {
            // Entrada inválida del cliente, no conflicto de estado: 400.
            throw new ValidationException(
                $"El paso {form.Paso} no es válido (debe estar entre {PasoMinimo} y {PasoMaximo}).");
        }

        var expediente = await CargarConAccesoAsync(expedienteId, ct);

        if (expediente.Estado == EstadoExpediente.Aprobado)
        {
            throw new BusinessRuleException(
                "No se pueden registrar subsanaciones en un expediente ya aprobado.");
        }

        // Espejo del CHECK CK_Subsanacion_JustificacionOArchivo, para dar un error amigable
        // en vez de que reviente el SaveChanges.
        var hayTexto = !string.IsNullOrWhiteSpace(form.TextoJustificacion);
        var hayArchivo = form.Archivo is { Length: > 0 };

        if (!hayTexto && !hayArchivo)
        {
            throw new ValidationException(
                "Debe proporcionar una justificación de texto o adjuntar un archivo para la subsanación.");
        }

        var ahora = DateTime.UtcNow;
        var usuarioId = _currentUser.GetUserId();

        var subsanacion = new Subsanacion
        {
            ExpedienteId = expedienteId,
            Paso = form.Paso,
            Campo = form.Campo,
            TextoJustificacion = hayTexto ? form.TextoJustificacion!.Trim() : null,
            UsuarioId = usuarioId,
            FechaCreacion = ahora,
            FechaSubsanacion = ahora
        };

        ArchivoGuardado? guardado = null;

        if (hayArchivo)
        {
            var validacion = await _validador.ValidarAsync(form.Archivo!, ct);

            if (!validacion.EsValido)
            {
                // Archivo vacío, muy grande, extensión no permitida o contenido que no coincide: 400.
                throw new ValidationException(validacion.Error!);
            }

            guardado = await _almacenamiento.GuardarAsync(form.Archivo!, validacion.Extension!, ct);

            subsanacion.ArchivoRuta = guardado.RutaRelativa;
            subsanacion.ArchivoNombre = form.Archivo!.FileName;
            // El MIME validado por contenido, no el que declaró el navegador.
            subsanacion.ArchivoMimeType = validacion.MimeDetectado;
            subsanacion.ArchivoExtension = validacion.Extension;
            subsanacion.ArchivoTamanoBytes = guardado.TamanoBytes;
            subsanacion.ArchivoHash = guardado.HashSha256;
        }

        _db.Subsanaciones.Add(subsanacion);

        _db.HistorialExpedientes.Add(new HistorialExpediente
        {
            ExpedienteId = expedienteId,
            UsuarioId = usuarioId,
            Accion = "Subsanacion",
            Detalle = $"Subsanación registrada — paso {form.Paso}, campo '{form.Campo}'",
            FechaCambio = ahora
        });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch
        {
            // El archivo ya está en disco: si la fila no entró, no dejarlo huérfano.
            if (guardado is not null)
            {
                await _almacenamiento.EliminarAsync(guardado.RutaRelativa, CancellationToken.None);
            }

            throw;
        }

        return MapearADto(subsanacion);
    }

    public async Task<List<SubsanacionDto>> ListarPorExpedienteAsync(int expedienteId, CancellationToken ct)
    {
        await CargarConAccesoAsync(expedienteId, ct);

        return await _db.Subsanaciones
            .AsNoTracking()
            .Where(s => s.ExpedienteId == expedienteId)
            .OrderByDescending(s => s.FechaSubsanacion)
            .Select(s => new SubsanacionDto
            {
                Id = s.Id,
                Paso = s.Paso,
                Campo = s.Campo,
                TextoJustificacion = s.TextoJustificacion,
                TieneArchivo = s.ArchivoRuta != null,
                ArchivoNombre = s.ArchivoNombre,
                ArchivoMimeType = s.ArchivoMimeType,
                ArchivoExtension = s.ArchivoExtension,
                ArchivoTamanoBytes = s.ArchivoTamanoBytes,
                ArchivoHash = s.ArchivoHash,
                FechaSubsanacion = s.FechaSubsanacion
            })
            .ToListAsync(ct);
    }

    public async Task<SubsanacionDto> ObtenerPorIdAsync(int expedienteId, int subsanacionId, CancellationToken ct)
    {
        await CargarConAccesoAsync(expedienteId, ct);

        var subsanacion = await _db.Subsanaciones
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ExpedienteId == expedienteId && s.Id == subsanacionId, ct);

        if (subsanacion is null)
        {
            throw new NotFoundException(
                $"No existe la subsanación {subsanacionId} en el expediente {expedienteId}.");
        }

        return MapearADto(subsanacion);
    }

    public async Task<(string rutaFisica, string nombreOriginal, string mimeType)> ObtenerArchivoAsync(
        int expedienteId,
        int subsanacionId,
        CancellationToken ct)
    {
        await CargarConAccesoAsync(expedienteId, ct);

        var subsanacion = await _db.Subsanaciones
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ExpedienteId == expedienteId && s.Id == subsanacionId, ct);

        if (subsanacion is null)
        {
            throw new NotFoundException(
                $"No existe la subsanación {subsanacionId} en el expediente {expedienteId}.");
        }

        if (subsanacion.ArchivoRuta is null)
        {
            throw new NotFoundException("Esta subsanación no tiene un archivo adjunto.");
        }

        var (rutaFisica, existe) = _almacenamiento.ResolverRutaFisica(subsanacion.ArchivoRuta);

        if (!existe)
        {
            throw new NotFoundException("El archivo no se encontró en el almacenamiento.");
        }

        return (
            rutaFisica,
            subsanacion.ArchivoNombre ?? Path.GetFileName(subsanacion.ArchivoRuta),
            subsanacion.ArchivoMimeType ?? MimePorDefecto);
    }

    public async Task EliminarAsync(int expedienteId, int subsanacionId, CancellationToken ct)
    {
        var subsanacion = await _db.Subsanaciones
            .Include(s => s.Expediente)
            .FirstOrDefaultAsync(s => s.ExpedienteId == expedienteId && s.Id == subsanacionId, ct);

        if (subsanacion is null)
        {
            throw new NotFoundException(
                $"No existe la subsanación {subsanacionId} en el expediente {expedienteId}.");
        }

        if (subsanacion.Expediente is not null)
        {
            AccesoExpediente.Verificar(subsanacion.Expediente, _currentUser);
        }

        if (subsanacion.Expediente?.Estado == EstadoExpediente.Aprobado)
        {
            throw new BusinessRuleException(
                "No se pueden eliminar subsanaciones de un expediente ya aprobado.");
        }

        if (subsanacion.ArchivoRuta is not null)
        {
            await _almacenamiento.EliminarAsync(subsanacion.ArchivoRuta, ct);
        }

        _db.Subsanaciones.Remove(subsanacion);

        _db.HistorialExpedientes.Add(new HistorialExpediente
        {
            ExpedienteId = expedienteId,
            UsuarioId = _currentUser.GetUserId(),
            Accion = "SubsanacionEliminada",
            Detalle = $"Subsanación {subsanacionId} eliminada — paso {subsanacion.Paso}, campo '{subsanacion.Campo}'",
            FechaCambio = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
    }

    // Carga el expediente y verifica que el usuario actual sea su dueño (o Admin).
    private async Task<Expediente> CargarConAccesoAsync(int expedienteId, CancellationToken ct)
    {
        var expediente = await _db.Expedientes.FirstOrDefaultAsync(e => e.Id == expedienteId, ct);

        if (expediente is null)
        {
            throw new NotFoundException($"No existe el expediente {expedienteId}.");
        }

        AccesoExpediente.Verificar(expediente, _currentUser);

        return expediente;
    }

    private static SubsanacionDto MapearADto(Subsanacion s) => new()
    {
        Id = s.Id,
        Paso = s.Paso,
        Campo = s.Campo,
        TextoJustificacion = s.TextoJustificacion,
        TieneArchivo = s.ArchivoRuta != null,
        ArchivoNombre = s.ArchivoNombre,
        ArchivoMimeType = s.ArchivoMimeType,
        ArchivoExtension = s.ArchivoExtension,
        ArchivoTamanoBytes = s.ArchivoTamanoBytes,
        ArchivoHash = s.ArchivoHash,
        FechaSubsanacion = s.FechaSubsanacion
    };
}
