using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Prodhab.Api.Configuracion;
using Prodhab.Api.Models;

namespace Prodhab.Api.Services;

// Arma el JWT firmado. Se construye el JwtSecurityToken directamente para que los claims
// viajen con el tipo exacto que espera la validación (sin el mapeo outbound del handler).
internal static class JwtTokenGenerator
{
    public static (string token, DateTime expiraEn) Generar(Usuario usuario, JwtOptions opciones)
    {
        var expiraEn = DateTime.UtcNow.AddHours(opciones.ExpiryHours);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Role, usuario.Rol),
            new("email", usuario.Email),
            new("name", usuario.Nombre)
        };

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opciones.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: opciones.Issuer,
            audience: opciones.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiraEn,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEn);
    }
}
