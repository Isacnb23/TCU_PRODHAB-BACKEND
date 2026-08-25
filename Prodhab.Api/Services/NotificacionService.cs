using Microsoft.EntityFrameworkCore;
using Prodhab.Api.Data;
using Prodhab.Api.DTOs.Notificaciones;
using Prodhab.Api.Exceptions;
using Prodhab.Api.Models;

namespace Prodhab.Api.Services;

public class NotificacionService : INotificacionService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public NotificacionService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task CrearAsync(int usuarioId, string mensaje, int? expedienteId, CancellationToken ct)
    {
        _db.Notificaciones.Add(new Notificacion
        {
            UsuarioId = usuarioId,
            Mensaje = mensaje,
            ExpedienteId = expedienteId,
            Leida = false,
            FechaCreacion = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<NotificacionDto>> ListarMiasAsync(CancellationToken ct)
    {
        var usuarioId = _currentUser.GetUserId();

        return await _db.Notificaciones
            .AsNoTracking()
            .Where(n => n.UsuarioId == usuarioId)
            .OrderByDescending(n => n.FechaCreacion)
            .Select(n => new NotificacionDto
            {
                Id = n.Id,
                Mensaje = n.Mensaje,
                ExpedienteId = n.ExpedienteId,
                Leida = n.Leida,
                FechaCreacion = n.FechaCreacion
            })
            .ToListAsync(ct);
    }

    public async Task<int> ContarNoLeidasAsync(CancellationToken ct)
    {
        var usuarioId = _currentUser.GetUserId();

        return await _db.Notificaciones
            .CountAsync(n => n.UsuarioId == usuarioId && !n.Leida, ct);
    }

    public async Task MarcarLeidaAsync(int id, CancellationToken ct)
    {
        var usuarioId = _currentUser.GetUserId();

        var notificacion = await _db.Notificaciones
            .FirstOrDefaultAsync(n => n.Id == id && n.UsuarioId == usuarioId, ct);

        if (notificacion is null)
        {
            throw new NotFoundException($"No existe la notificación {id}.");
        }

        notificacion.Leida = true;

        await _db.SaveChangesAsync(ct);
    }
}
