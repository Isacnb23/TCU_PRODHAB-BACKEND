using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Prodhab.Api.Data;
using Prodhab.Api.DTOs.Usuarios;
using Prodhab.Api.Exceptions;
using Prodhab.Api.Models;

namespace Prodhab.Api.Services;

public class UsuarioService : IUsuarioService
{
    private static readonly string[] RolesPermitidos = ["Admin", "Usuario"];

    // Sin caracteres ambiguos: 0/O, 1/l/I quedan fuera para que la contraseña
    // temporal se pueda leer y transcribir en voz alta sin confusiones.
    private const string CaracteresPasswordTemporal =
        "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";

    private const int LongitudPasswordTemporal = 10;

    private readonly AppDbContext _db;

    public UsuarioService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UsuarioDto> CrearAsync(CrearUsuarioDto dto, CancellationToken ct)
    {
        if (!RolesPermitidos.Contains(dto.Rol))
        {
            throw new ValidationException(
                $"El rol '{dto.Rol}' no es válido (debe ser {string.Join(" o ", RolesPermitidos)}).");
        }

        var email = dto.Email.Trim();

        if (await _db.Usuarios.AnyAsync(u => u.Email.ToLower() == email.ToLower(), ct))
        {
            throw new BusinessRuleException("Ya existe un usuario con ese correo.");
        }

        var usuario = new Usuario
        {
            Nombre = dto.Nombre.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Rol = dto.Rol,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync(ct);

        return MapearADto(usuario);
    }

    public async Task<List<UsuarioDto>> ListarAsync(CancellationToken ct)
    {
        return await _db.Usuarios
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .Select(u => new UsuarioDto
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Email = u.Email,
                Rol = u.Rol,
                Activo = u.Activo,
                FechaCreacion = u.FechaCreacion
            })
            .ToListAsync(ct);
    }

    public async Task DesactivarAsync(int id, CancellationToken ct)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

        if (usuario is null)
        {
            throw new NotFoundException($"No existe el usuario {id}.");
        }

        // Los usuarios no se borran: quedan inactivos para preservar la trazabilidad.
        usuario.Activo = false;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<UsuarioDto> ObtenerPorIdAsync(int id, CancellationToken ct)
    {
        var usuario = await _db.Usuarios
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UsuarioDto
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Email = u.Email,
                Rol = u.Rol,
                Activo = u.Activo,
                FechaCreacion = u.FechaCreacion
            })
            .FirstOrDefaultAsync(ct);

        if (usuario is null)
        {
            throw new NotFoundException($"No existe el usuario {id}.");
        }

        return usuario;
    }

    public async Task<string> ResetearPasswordAsync(int id, CancellationToken ct)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

        if (usuario is null)
        {
            throw new NotFoundException($"No existe el usuario {id}.");
        }

        var passwordTemporal = GenerarPasswordTemporal();
        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordTemporal);
        await _db.SaveChangesAsync(ct);

        // Única vez que la contraseña existe en texto plano: se devuelve al Admin
        // para que la comunique por fuera del sistema, nunca se guarda así.
        return passwordTemporal;
    }

    private static string GenerarPasswordTemporal()
    {
        var caracteres = RandomNumberGenerator.GetItems<char>(CaracteresPasswordTemporal, LongitudPasswordTemporal);
        return new string(caracteres);
    }

    private static UsuarioDto MapearADto(Usuario u) => new()
    {
        Id = u.Id,
        Nombre = u.Nombre,
        Email = u.Email,
        Rol = u.Rol,
        Activo = u.Activo,
        FechaCreacion = u.FechaCreacion
    };
}
