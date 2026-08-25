using Microsoft.EntityFrameworkCore;
using Prodhab.Api.Data;
using Prodhab.Api.DTOs.Expedientes;
using Prodhab.Api.DTOs.Revision;
using Prodhab.Api.Exceptions;
using Prodhab.Api.Models;

namespace Prodhab.Api.Services;

public class RevisionService : IRevisionService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IExpedienteService _expedientes;

    public RevisionService(AppDbContext db, ICurrentUserService currentUser, IExpedienteService expedientes)
    {
        _db = db;
        _currentUser = currentUser;
        _expedientes = expedientes;
    }

    public async Task<ExpedienteDetalleDto> SolicitarSubsanacionAsync(int id, SolicitarSubsanacionDto dto, CancellationToken ct)
    {
        var expediente = await CargarConAccesoAsync(id, ct);

        if (expediente.Estado != EstadoExpediente.Enviado)
        {
            throw new BusinessRuleException(
                $"Solo se puede solicitar subsanación de un expediente en estado Enviado (actual: {expediente.Estado}).");
        }

        foreach (var observacion in dto.Observaciones)
        {
            if (observacion.Paso < 1 || observacion.Paso > 9)
            {
                throw new ValidationException($"El paso {observacion.Paso} no es válido (debe estar entre 1 y 9).");
            }

            if (string.IsNullOrWhiteSpace(observacion.Campo))
            {
                throw new ValidationException("Cada observación debe indicar a qué campo corresponde.");
            }
        }

        var ahora = DateTime.UtcNow;
        var adminId = _currentUser.GetUserId();

        foreach (var observacion in dto.Observaciones)
        {
            _db.Observaciones.Add(new Observacion
            {
                ExpedienteId = id,
                Paso = observacion.Paso,
                Texto = observacion.Texto,
                Campo = observacion.Campo,
                UsuarioId = adminId,
                FechaCreacion = ahora
            });
        }

        expediente.Estado = EstadoExpediente.RequiereSubsanacion;
        expediente.FechaModificacion = ahora;

        _db.HistorialExpedientes.Add(new HistorialExpediente
        {
            ExpedienteId = id,
            UsuarioId = adminId,
            Accion = "SolicitudSubsanacion",
            Detalle = $"Se solicitaron subsanaciones en {dto.Observaciones.Count} paso(s)",
            FechaCambio = ahora
        });

        await _db.SaveChangesAsync(ct);

        return await _expedientes.ObtenerPorIdAsync(id, ct);
    }

    public async Task<ExpedienteDetalleDto> AprobarAsync(int id, AprobarDto dto, CancellationToken ct)
    {
        var expediente = await CargarConAccesoAsync(id, ct);

        if (expediente.Estado != EstadoExpediente.Enviado)
        {
            throw new BusinessRuleException(
                $"Solo se puede aprobar un expediente en estado Enviado (actual: {expediente.Estado}).");
        }

        // El índice único filtrado es la red de seguridad; este chequeo previo es para dar un
        // mensaje amigable en vez de que reviente el SaveChanges.
        var yaExiste = await _db.Expedientes
            .AnyAsync(e => e.Id != id && e.NumeroExpediente == dto.NumeroExpediente, ct);

        if (yaExiste)
        {
            throw new BusinessRuleException($"Ya existe un expediente con el número {dto.NumeroExpediente}.");
        }

        var ahora = DateTime.UtcNow;

        expediente.Estado = EstadoExpediente.Aprobado;
        expediente.NumeroExpediente = dto.NumeroExpediente;
        expediente.FechaModificacion = ahora;

        _db.HistorialExpedientes.Add(new HistorialExpediente
        {
            ExpedienteId = id,
            UsuarioId = _currentUser.GetUserId(),
            Accion = "Aprobacion",
            Detalle = $"Expediente aprobado con número {dto.NumeroExpediente}",
            FechaCambio = ahora
        });

        await _db.SaveChangesAsync(ct);

        return await _expedientes.ObtenerPorIdAsync(id, ct);
    }

    // Carga el expediente y verifica que el usuario actual sea su dueño (o Admin).
    private async Task<Expediente> CargarConAccesoAsync(int id, CancellationToken ct)
    {
        var expediente = await _db.Expedientes.FirstOrDefaultAsync(e => e.Id == id, ct);

        if (expediente is null)
        {
            throw new NotFoundException($"No existe el expediente {id}.");
        }

        AccesoExpediente.Verificar(expediente, _currentUser);

        return expediente;
    }
}
