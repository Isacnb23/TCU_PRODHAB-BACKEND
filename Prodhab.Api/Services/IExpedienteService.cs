using Prodhab.Api.DTOs.Expedientes;

namespace Prodhab.Api.Services;

public interface IExpedienteService
{
    Task<ExpedienteDetalleDto> CrearAsync(CrearExpedienteDto dto, CancellationToken ct);

    Task<List<ExpedienteListaDto>> ListarPorUsuarioAsync(int usuarioId, CancellationToken ct);

    Task<ExpedienteDetalleDto> ObtenerPorIdAsync(int id, CancellationToken ct);

    Task ActualizarAsync(int id, ActualizarExpedienteDto dto, CancellationToken ct);

    Task EliminarAsync(int id, CancellationToken ct);
}
