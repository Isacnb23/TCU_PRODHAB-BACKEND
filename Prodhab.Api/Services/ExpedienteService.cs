using Microsoft.EntityFrameworkCore;
using Prodhab.Api.Data;
using Prodhab.Api.DTOs.Expedientes;
using Prodhab.Api.DTOs.Revision;
using Prodhab.Api.Exceptions;
using Prodhab.Api.Models;

namespace Prodhab.Api.Services;

public class ExpedienteService : IExpedienteService
{
    // El paso 9 es la revisión/envío en sí: no se exige "completado" para poder enviar.
    private static readonly int[] PasosRequeridos = { 1, 2, 3, 4, 5, 6, 7, 8 };

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ExpedienteService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ExpedienteDetalleDto> CrearAsync(CrearExpedienteDto dto, CancellationToken ct)
    {
        var ahora = DateTime.UtcNow;

        var expediente = new Expediente
        {
            Entidad = dto.Entidad,
            Anio = dto.Anio,
            Estado = EstadoExpediente.Borrador,
            PasoActual = 1,
            NumeroExpediente = null,
            UsuarioId = _currentUser.GetUserId(),
            FechaCreacion = ahora,
            FechaModificacion = ahora
        };

        // Por la navegación, EF resuelve el ExpedienteId del historial en el mismo SaveChanges.
        expediente.Historial.Add(new HistorialExpediente
        {
            UsuarioId = expediente.UsuarioId,
            Accion = "Creacion",
            Detalle = "Expediente creado en borrador",
            FechaCambio = ahora
        });

        _db.Expedientes.Add(expediente);
        await _db.SaveChangesAsync(ct);

        // Recién creado: no tiene DatosFormulario todavía.
        return new ExpedienteDetalleDto
        {
            Id = expediente.Id,
            NumeroExpediente = expediente.NumeroExpediente,
            Entidad = expediente.Entidad,
            Anio = expediente.Anio,
            Estado = expediente.Estado.ToString(),
            PasoActual = expediente.PasoActual,
            FechaCreacion = expediente.FechaCreacion,
            FechaModificacion = expediente.FechaModificacion,
            FechaEnvio = expediente.FechaEnvio,
            Datos = new List<DatosFormularioDto>(),
            Observaciones = new List<ObservacionDto>()
        };
    }

    public async Task<List<ExpedienteListaDto>> ListarPorUsuarioAsync(int usuarioId, string? estado, CancellationToken ct)
    {
        // Un Admin ve todos los expedientes; el resto, solo los propios.
        // El controller siempre pasa el usuario autenticado.
        var consulta = _db.Expedientes.AsNoTracking();

        if (!_currentUser.EsAdmin())
        {
            consulta = consulta.Where(e => e.UsuarioId == usuarioId);
        }

        // Filtro opcional por estado (ej. panel Admin: ?estado=Enviado). Si no matchea un estado
        // válido, se ignora y se devuelve sin filtrar.
        if (!string.IsNullOrWhiteSpace(estado) && Enum.TryParse<EstadoExpediente>(estado, true, out var estadoFiltro))
        {
            consulta = consulta.Where(e => e.Estado == estadoFiltro);
        }

        return await consulta
            .OrderByDescending(e => e.FechaModificacion)
            .Select(e => new ExpedienteListaDto
            {
                Id = e.Id,
                NumeroExpediente = e.NumeroExpediente,
                Entidad = e.Entidad,
                Anio = e.Anio,
                Estado = e.Estado.ToString(),
                PasoActual = e.PasoActual,
                FechaCreacion = e.FechaCreacion,
                FechaModificacion = e.FechaModificacion
            })
            .ToListAsync(ct);
    }

    public async Task<ExpedienteDetalleDto> ObtenerPorIdAsync(int id, CancellationToken ct)
    {
        await CargarConAccesoAsync(id, ct);

        var expediente = await _db.Expedientes
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new ExpedienteDetalleDto
            {
                Id = e.Id,
                NumeroExpediente = e.NumeroExpediente,
                Entidad = e.Entidad,
                Anio = e.Anio,
                Estado = e.Estado.ToString(),
                PasoActual = e.PasoActual,
                FechaCreacion = e.FechaCreacion,
                FechaModificacion = e.FechaModificacion,
                FechaEnvio = e.FechaEnvio,
                Datos = e.Datos
                    .OrderBy(d => d.Paso)
                    .Select(d => new DatosFormularioDto
                    {
                        Paso = d.Paso,
                        DatosJson = d.DatosJson,
                        Completado = d.Completado,
                        FechaActualizacion = d.FechaActualizacion
                    })
                    .ToList(),
                Observaciones = e.Observaciones
                    .OrderByDescending(o => o.FechaCreacion)
                    .Select(o => new ObservacionDto
                    {
                        Id = o.Id,
                        Paso = o.Paso,
                        Texto = o.Texto,
                        FechaCreacion = o.FechaCreacion
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (expediente is null)
        {
            throw new NotFoundException($"No existe el expediente {id}.");
        }

        return expediente;
    }

    public async Task ActualizarAsync(int id, ActualizarExpedienteDto dto, CancellationToken ct)
    {
        var expediente = await CargarConAccesoAsync(id, ct);

        if (expediente.Estado != EstadoExpediente.Borrador)
        {
            throw new BusinessRuleException("Solo se pueden modificar expedientes en estado Borrador.");
        }

        var ahora = DateTime.UtcNow;

        expediente.Entidad = dto.Entidad;
        expediente.Anio = dto.Anio;
        expediente.FechaModificacion = ahora;

        _db.HistorialExpedientes.Add(new HistorialExpediente
        {
            ExpedienteId = expediente.Id,
            UsuarioId = _currentUser.GetUserId(),
            Accion = "Actualizacion",
            Detalle = "Se actualizó la cabecera del expediente",
            FechaCambio = ahora
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task EliminarAsync(int id, CancellationToken ct)
    {
        var expediente = await CargarConAccesoAsync(id, ct);

        if (expediente.Estado != EstadoExpediente.Borrador)
        {
            throw new BusinessRuleException("No se puede eliminar un expediente que ya fue enviado a PRODHAB.");
        }

        // Datos, Subsanaciones e Historial caen por cascade.
        _db.Expedientes.Remove(expediente);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ExpedienteDetalleDto> EnviarAsync(int id, CancellationToken ct)
    {
        var expediente = await CargarConAccesoAsync(id, ct);

        if (expediente.Estado != EstadoExpediente.Borrador && expediente.Estado != EstadoExpediente.RequiereSubsanacion)
        {
            throw new BusinessRuleException($"No se puede enviar un expediente en estado {expediente.Estado}.");
        }

        var pasosCompletados = await _db.DatosFormularios
            .Where(d => d.ExpedienteId == id && d.Completado)
            .Select(d => d.Paso)
            .ToListAsync(ct);

        var pasosFaltantes = PasosRequeridos.Except(pasosCompletados).OrderBy(p => p).ToList();

        if (pasosFaltantes.Count > 0)
        {
            throw new BusinessRuleException(
                $"No se puede enviar el expediente. Faltan completar los pasos: {string.Join(", ", pasosFaltantes)}.");
        }

        var ahora = DateTime.UtcNow;

        expediente.Estado = EstadoExpediente.Enviado;
        expediente.FechaEnvio = ahora;
        expediente.FechaModificacion = ahora;

        _db.HistorialExpedientes.Add(new HistorialExpediente
        {
            ExpedienteId = expediente.Id,
            UsuarioId = _currentUser.GetUserId(),
            Accion = "Envio",
            Detalle = "Expediente enviado a PRODHAB",
            FechaCambio = ahora
        });

        await _db.SaveChangesAsync(ct);

        return await ObtenerPorIdAsync(id, ct);
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
