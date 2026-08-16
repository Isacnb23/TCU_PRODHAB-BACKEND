using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prodhab.Api.DTOs.Auth;
using Prodhab.Api.DTOs.Usuarios;
using Prodhab.Api.Services;

namespace Prodhab.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IUsuarioService _usuarios;
    private readonly ICurrentUserService _currentUser;

    public AuthController(
        IAuthService auth,
        IUsuarioService usuarios,
        ICurrentUserService currentUser)
    {
        _auth = auth;
        _usuarios = usuarios;
        _currentUser = currentUser;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody] LoginDto dto,
        CancellationToken ct)
    {
        var respuesta = await _auth.LoginAsync(dto, ct);
        return Ok(respuesta);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioDto>> Yo(CancellationToken ct)
    {
        var usuario = await _usuarios.ObtenerPorIdAsync(_currentUser.GetUserId(), ct);
        return Ok(usuario);
    }
}
