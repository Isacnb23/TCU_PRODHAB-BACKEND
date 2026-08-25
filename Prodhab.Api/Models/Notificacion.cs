using System.ComponentModel.DataAnnotations;

namespace Prodhab.Api.Models;

public class Notificacion
{
    public int Id { get; set; }

    // Destinatario. Columna simple, sin navegación, para evitar cascade paths.
    public int UsuarioId { get; set; }

    [Required]
    [MaxLength(300)]
    public string Mensaje { get; set; } = string.Empty;

    public int? ExpedienteId { get; set; }

    public bool Leida { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public Expediente? Expediente { get; set; }
}
