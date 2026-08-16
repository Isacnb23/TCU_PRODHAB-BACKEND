using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prodhab.Api.DTOs.Expedientes;
using Prodhab.Api.Services;

namespace Prodhab.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/expedientes/{expedienteId:int}/pasos")]
public class PasosController : ControllerBase
{
    private readonly IDatosFormularioService _pasos;

    public PasosController(IDatosFormularioService pasos)
    {
        _pasos = pasos;
    }

    [HttpPut("{paso:int}")]
    [ProducesResponseType(typeof(DatosFormularioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DatosFormularioDto>> Guardar(
        int expedienteId,
        int paso,
        [FromBody] GuardarPasoDto dto,
        CancellationToken ct)
    {
        var guardado = await _pasos.GuardarPasoAsync(expedienteId, paso, dto, ct);
        return Ok(guardado);
    }

    [HttpGet("{paso:int}")]
    [ProducesResponseType(typeof(DatosFormularioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DatosFormularioDto>> Obtener(
        int expedienteId,
        int paso,
        CancellationToken ct)
    {
        var datos = await _pasos.ObtenerPasoAsync(expedienteId, paso, ct);
        return Ok(datos);
    }
}
