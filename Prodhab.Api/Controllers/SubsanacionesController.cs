using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prodhab.Api.DTOs.Subsanaciones;
using Prodhab.Api.Services;

namespace Prodhab.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/expedientes/{expedienteId:int}/subsanaciones")]
public class SubsanacionesController : ControllerBase
{
    private readonly ISubsanacionService _subsanaciones;

    public SubsanacionesController(ISubsanacionService subsanaciones)
    {
        _subsanaciones = subsanaciones;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    // 10 MB del archivo + margen para el resto del multipart.
    [RequestSizeLimit(12_000_000)]
    [ProducesResponseType(typeof(SubsanacionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubsanacionDto>> Crear(
        int expedienteId,
        [FromForm] CrearSubsanacionForm form,
        CancellationToken ct)
    {
        var creada = await _subsanaciones.CrearAsync(expedienteId, form, ct);

        return CreatedAtAction(
            nameof(ObtenerPorId),
            new { expedienteId, subsanacionId = creada.Id },
            creada);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<SubsanacionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<SubsanacionDto>>> Listar(int expedienteId, CancellationToken ct)
    {
        var subsanaciones = await _subsanaciones.ListarPorExpedienteAsync(expedienteId, ct);
        return Ok(subsanaciones);
    }

    [HttpGet("{subsanacionId:int}")]
    [ProducesResponseType(typeof(SubsanacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubsanacionDto>> ObtenerPorId(
        int expedienteId,
        int subsanacionId,
        CancellationToken ct)
    {
        var subsanacion = await _subsanaciones.ObtenerPorIdAsync(expedienteId, subsanacionId, ct);
        return Ok(subsanacion);
    }

    [HttpGet("{subsanacionId:int}/archivo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Descargar(
        int expedienteId,
        int subsanacionId,
        CancellationToken ct)
    {
        var (rutaFisica, nombreOriginal, mimeType) =
            await _subsanaciones.ObtenerArchivoAsync(expedienteId, subsanacionId, ct);

        // El content-type es el VALIDADO por contenido; el nombre de descarga, el original del usuario.
        return PhysicalFile(rutaFisica, mimeType, nombreOriginal);
    }

    [HttpDelete("{subsanacionId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(
        int expedienteId,
        int subsanacionId,
        CancellationToken ct)
    {
        await _subsanaciones.EliminarAsync(expedienteId, subsanacionId, ct);
        return NoContent();
    }
}
