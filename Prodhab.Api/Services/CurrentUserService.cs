using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Prodhab.Api.Exceptions;

namespace Prodhab.Api.Services;

// Resuelve el usuario actual desde los claims del JWT de la petición: no consulta la BD.
public class CurrentUserService : ICurrentUserService
{
    public const string RolAdmin = "Admin";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int GetUserId()
    {
        var usuario = _httpContextAccessor.HttpContext?.User;

        var valor = usuario?.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? usuario?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(valor, out var id))
        {
            throw new UnauthorizedException("No hay un usuario autenticado.");
        }

        return id;
    }

    public string GetRol()
    {
        var usuario = _httpContextAccessor.HttpContext?.User;

        var rol = usuario?.FindFirstValue(ClaimTypes.Role);

        if (string.IsNullOrWhiteSpace(rol))
        {
            throw new UnauthorizedException("No hay un usuario autenticado.");
        }

        return rol;
    }

    public bool EsAdmin() => GetRol() == RolAdmin;
}
