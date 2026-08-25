using Prodhab.Api.DTOs.Expedientes;
using Prodhab.Api.DTOs.Revision;

namespace Prodhab.Api.Services;

public interface IRevisionService
{
    Task<ExpedienteDetalleDto> SolicitarSubsanacionAsync(int id, SolicitarSubsanacionDto dto, CancellationToken ct);

    Task<ExpedienteDetalleDto> AprobarAsync(int id, AprobarDto dto, CancellationToken ct);

    Task<string> SugerirNumeroAsync(int id, CancellationToken ct);
}
