using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Prodhab.Api.Configuracion;
using Prodhab.Api.Data;
using Prodhab.Api.DTOs.Auth;
using Prodhab.Api.Exceptions;

namespace Prodhab.Api.Services;

public class AuthService : IAuthService
{
    // Mismo mensaje para usuario inexistente y password incorrecto: no revelar cuál falló.
    private const string CredencialesInvalidas = "Credenciales inválidas.";

    private readonly AppDbContext _db;
    private readonly JwtOptions _jwt;

    public AuthService(AppDbContext db, IOptions<JwtOptions> jwt)
    {
        _db = db;
        _jwt = jwt.Value;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto dto, CancellationToken ct)
    {
        var email = dto.Email.Trim().ToLower();

        var usuario = await _db.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.Activo, ct);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
        {
            throw new UnauthorizedException(CredencialesInvalidas);
        }

        var (token, expiraEn) = JwtTokenGenerator.Generar(usuario, _jwt);

        return new LoginResponseDto
        {
            Token = token,
            ExpiraEn = expiraEn,
            UsuarioId = usuario.Id,
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            Rol = usuario.Rol
        };
    }
}
