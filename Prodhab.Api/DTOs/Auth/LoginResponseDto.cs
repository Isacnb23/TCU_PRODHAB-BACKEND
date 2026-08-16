namespace Prodhab.Api.DTOs.Auth;

// Respuesta del login: el token y lo mínimo del usuario para que el front arme la sesión.
public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiraEn { get; set; }

    public int UsuarioId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Rol { get; set; } = string.Empty;
}
