using Prodhab.Api.DTOs.Expedientes;

namespace Prodhab.Api.Services;

public interface IDatosFormularioService
{
    Task<DatosFormularioDto> GuardarPasoAsync(int expedienteId, int paso, GuardarPasoDto dto, CancellationToken ct);

    Task<DatosFormularioDto> ObtenerPasoAsync(int expedienteId, int paso, CancellationToken ct);
}
