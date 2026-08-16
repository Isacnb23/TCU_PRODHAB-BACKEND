using System.ComponentModel.DataAnnotations;

namespace Prodhab.Api.Models;

public class Usuario
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    // Hash BCrypt de la contraseña.
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    // Usuario / Admin
    [MaxLength(50)]
    public string Rol { get; set; } = "Usuario";

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
