using Microsoft.EntityFrameworkCore;
using Prodhab.Api.Data;
using Prodhab.Api.DTOs.Expedientes;
using Prodhab.Api.Exceptions;
using Prodhab.Api.Models;

namespace Prodhab.Api.Services;

public class DatosFormularioService : IDatosFormularioService
{
    private const int PasoMinimo = 1;
    private const int PasoMaximo = 9;

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DatosFormularioService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DatosFormularioDto> GuardarPasoAsync(
        int expedienteId,
        int paso,
        GuardarPasoDto dto,
        CancellationToken ct)
    {
        ValidarRangoPaso(paso);

        var expediente = await _db.Expedientes.FirstOrDefaultAsync(e => e.Id == expedienteId, ct);

        if (expediente is null)
        {
            throw new NotFoundException($"No existe el expediente {expedienteId}.");
        }

        AccesoExpediente.Verificar(expediente, _currentUser);

        if (expediente.Estado != EstadoExpediente.Borrador)
        {
            throw new BusinessRuleException("Solo se pueden editar los pasos de un expediente en estado Borrador.");
        }

        // Upsert sobre la fila única (ExpedienteId, Paso).
        var fila = await _db.DatosFormularios
            .FirstOrDefaultAsync(d => d.ExpedienteId == expedienteId && d.Paso == paso, ct);

        var completadoPrevio = fila?.Completado ?? false;
        var ahora = DateTime.UtcNow;

        if (fila is null)
        {
            fila = new DatosFormulario
            {
                ExpedienteId = expedienteId,
                Paso = paso
            };

            _db.DatosFormularios.Add(fila);
        }

        fila.DatosJson = dto.DatosJson;
        fila.Completado = dto.Completado;
        fila.FechaActualizacion = ahora;

        expediente.PasoActual = Math.Max(expediente.PasoActual, paso);
        expediente.FechaModificacion = ahora;

        // Solo la transición false -> true entra a la bitácora: los autosaves no la inundan.
        if (!completadoPrevio && dto.Completado)
        {
            _db.HistorialExpedientes.Add(new HistorialExpediente
            {
                ExpedienteId = expedienteId,
                UsuarioId = expediente.UsuarioId,
                Accion = "PasoCompletado",
                Detalle = $"Paso {paso} completado",
                FechaCambio = ahora
            });
        }

        await _db.SaveChangesAsync(ct);

        return new DatosFormularioDto
        {
            Paso = fila.Paso,
            DatosJson = fila.DatosJson,
            Completado = fila.Completado,
            FechaActualizacion = fila.FechaActualizacion
        };
    }

    public async Task<DatosFormularioDto> ObtenerPasoAsync(int expedienteId, int paso, CancellationToken ct)
    {
        ValidarRangoPaso(paso);

        // El expediente se carga primero para poder verificar la propiedad antes de leer sus datos.
        var expediente = await _db.Expedientes
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == expedienteId, ct);

        if (expediente is null)
        {
            throw new NotFoundException($"No existe el expediente {expedienteId}.");
        }

        AccesoExpediente.Verificar(expediente, _currentUser);

        var datos = await _db.DatosFormularios
            .AsNoTracking()
            .Where(d => d.ExpedienteId == expedienteId && d.Paso == paso)
            .Select(d => new DatosFormularioDto
            {
                Paso = d.Paso,
                DatosJson = d.DatosJson,
                Completado = d.Completado,
                FechaActualizacion = d.FechaActualizacion
            })
            .FirstOrDefaultAsync(ct);

        if (datos is not null)
        {
            return datos;
        }

        // El expediente ya se validó arriba: si no hay fila, es que el paso aún no se guardó.
        throw new NotFoundException($"El paso {paso} del expediente {expedienteId} aún no tiene datos guardados.");
    }

    private static void ValidarRangoPaso(int paso)
    {
        if (paso < PasoMinimo || paso > PasoMaximo)
        {
            // Entrada inválida del cliente, no conflicto de estado: 400.
            throw new ValidationException(
                $"El paso {paso} no es válido (debe estar entre {PasoMinimo} y {PasoMaximo}).");
        }
    }
}
