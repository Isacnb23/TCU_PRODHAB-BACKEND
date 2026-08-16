using Prodhab.Api.DTOs.Subsanaciones;

namespace Prodhab.Api.Services;

public interface ISubsanacionService
{
    Task<SubsanacionDto> CrearAsync(int expedienteId, CrearSubsanacionForm form, CancellationToken ct);

    Task<List<SubsanacionDto>> ListarPorExpedienteAsync(int expedienteId, CancellationToken ct);

    Task<SubsanacionDto> ObtenerPorIdAsync(int expedienteId, int subsanacionId, CancellationToken ct);

    Task<(string rutaFisica, string nombreOriginal, string mimeType)> ObtenerArchivoAsync(
        int expedienteId,
        int subsanacionId,
        CancellationToken ct);

    Task EliminarAsync(int expedienteId, int subsanacionId, CancellationToken ct);
}
