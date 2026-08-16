using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prodhab.Api.DTOs.Expedientes;
using Prodhab.Api.Services;

namespace Prodhab.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/expedientes")]
public class ExpedientesController : ControllerBase
{
    private readonly IExpedienteService _expedientes;
    private readonly ICurrentUserService _currentUser;

    public ExpedientesController(IExpedienteService expedientes, ICurrentUserService currentUser)
    {
        _expedientes = expedientes;
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
    public async Task<ActionResult<List<ExpedienteListaDto>>> Listar(CancellationToken ct)
    {
        var expedientes = await _expedientes.ListarPorUsuarioAsync(_currentUser.GetUserId(), ct);
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
}
