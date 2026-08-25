using Prodhab.Api.DTOs.Notificaciones;

namespace Prodhab.Api.Services;

public interface INotificacionService
{
    Task CrearAsync(int usuarioId, string mensaje, int? expedienteId, CancellationToken ct);

    Task<List<NotificacionDto>> ListarMiasAsync(CancellationToken ct);

    Task<int> ContarNoLeidasAsync(CancellationToken ct);

    Task MarcarLeidaAsync(int id, CancellationToken ct);
}
