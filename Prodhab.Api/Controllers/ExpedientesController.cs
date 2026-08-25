using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prodhab.Api.DTOs.Expedientes;
using Prodhab.Api.DTOs.Revision;
using Prodhab.Api.Services;

namespace Prodhab.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/expedientes")]
public class ExpedientesController : ControllerBase
{
    private readonly IExpedienteService _expedientes;
    private readonly IRevisionService _revision;
    private readonly ICurrentUserService _currentUser;

    public ExpedientesController(IExpedienteService expedientes, IRevisionService revision, ICurrentUserService currentUser)
    {
        _expedientes = expedientes;
        _revision = revision;
        _currentUser = currentUser;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ExpedienteDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExpedienteDetalleDto>> Crear(
        [FromBody] CrearExpedienteDto dto,
        CancellationToken ct)
    {
        var creado = await _expedientes.CrearAsync(dto, ct);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = creado.Id }, creado);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ExpedienteListaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ExpedienteListaDto>>> Listar([FromQuery] string? estado, CancellationToken ct)
    {
        var expedientes = await _expedientes.ListarPorUsuarioAsync(_currentUser.GetUserId(), estado, ct);
        return Ok(expedientes);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ExpedienteDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExpedienteDetalleDto>> ObtenerPorId(int id, CancellationToken ct)
    {
        var expediente = await _expedientes.ObtenerPorIdAsync(id, ct);
        return Ok(expediente);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(
        int id,
        [FromBody] ActualizarExpedienteDto dto,
        CancellationToken ct)
    {
        await _expedientes.ActualizarAsync(id, dto, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await _expedientes.EliminarAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/enviar")]
    [ProducesResponseType(typeof(ExpedienteDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExpedienteDetalleDto>> Enviar(int id, CancellationToken ct)
    {
        var enviado = await _expedientes.EnviarAsync(id, ct);
        return Ok(enviado);
    }

    [HttpPost("{id:int}/solicitar-subsanacion")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ExpedienteDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExpedienteDetalleDto>> SolicitarSubsanacion(
        int id,
        [FromBody] SolicitarSubsanacionDto dto,
        CancellationToken ct)
    {
        var actualizado = await _revision.SolicitarSubsanacionAsync(id, dto, ct);
        return Ok(actualizado);
    }

    [HttpGet("{id:int}/sugerir-numero")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SugerirNumeroDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SugerirNumeroDto>> SugerirNumero(int id, CancellationToken ct)
    {
        var numeroSugerido = await _revision.SugerirNumeroAsync(id, ct);
        return Ok(new SugerirNumeroDto { NumeroSugerido = numeroSugerido });
    }

    [HttpPost("{id:int}/aprobar")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ExpedienteDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExpedienteDetalleDto>> Aprobar(
        int id,
        [FromBody] AprobarDto dto,
        CancellationToken ct)
    {
        var actualizado = await _revision.AprobarAsync(id, dto, ct);
        return Ok(actualizado);
    }
}
