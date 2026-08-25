using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prodhab.Api.DTOs.Notificaciones;
using Prodhab.Api.Services;

namespace Prodhab.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notificaciones")]
public class NotificacionesController : ControllerBase
{
    private readonly INotificacionService _notificaciones;

    public NotificacionesController(INotificacionService notificaciones)
    {
        _notificaciones = notificaciones;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<NotificacionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<NotificacionDto>>> Listar(CancellationToken ct)
    {
        var notificaciones = await _notificaciones.ListarMiasAsync(ct);
        return Ok(notificaciones);
    }

    [HttpGet("no-leidas/count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ContarNoLeidas(CancellationToken ct)
    {
        var count = await _notificaciones.ContarNoLeidasAsync(ct);
        return Ok(new { count });
    }

    [HttpPatch("{id:int}/leer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarcarLeida(int id, CancellationToken ct)
    {
        await _notificaciones.MarcarLeidaAsync(id, ct);
        return NoContent();
    }
}
