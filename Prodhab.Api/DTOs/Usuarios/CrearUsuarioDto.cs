using System.ComponentModel.DataAnnotations;

namespace Prodhab.Api.DTOs.Usuarios;

public class CrearUsuarioDto
{
    [Required]
    [MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    // "Admin" o "Usuario": el valor concreto lo valida el servicio.
    [Required]
    public string Rol { get; set; } = string.Empty;
}
