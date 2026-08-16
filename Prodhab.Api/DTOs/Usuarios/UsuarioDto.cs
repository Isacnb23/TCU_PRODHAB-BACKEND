namespace Prodhab.Api.DTOs.Usuarios;

// Respuesta pública: nunca incluye PasswordHash.
public class UsuarioDto
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Rol { get; set; } = string.Empty;

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }
}
