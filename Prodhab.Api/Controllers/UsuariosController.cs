using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prodhab.Api.DTOs.Usuarios;
using Prodhab.Api.Services;

namespace Prodhab.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarios;

    public UsuariosController(IUsuarioService usuarios)
    {
        _usuarios = usuarios;
    }

    [HttpPost]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioDto>> Crear(
        [FromBody] CrearUsuarioDto dto,
        CancellationToken ct)
    {
        var creado = await _usuarios.CrearAsync(dto, ct);
        return CreatedAtAction(nameof(Listar), new { }, creado);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<UsuarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<UsuarioDto>>> Listar(CancellationToken ct)
    {
        var usuarios = await _usuarios.ListarAsync(ct);
        return Ok(usuarios);
    }

    [HttpPatch("{id:int}/desactivar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desactivar(int id, CancellationToken ct)
    {
        await _usuarios.DesactivarAsync(id, ct);
        return NoContent();
    }

    [HttpPatch("{id:int}/resetear-password")]
    [ProducesResponseType(typeof(ResetearPasswordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResetearPasswordDto>> ResetearPassword(int id, CancellationToken ct)
    {
        var passwordTemporal = await _usuarios.ResetearPasswordAsync(id, ct);
        return Ok(new ResetearPasswordDto { PasswordTemporal = passwordTemporal });
    }
}
