using System.ComponentModel.DataAnnotations;

namespace Prodhab.Api.Models;

public class HistorialExpediente
{
    public int Id { get; set; }

    public int ExpedienteId { get; set; }

    public int UsuarioId { get; set; }

    // Ej: "CambioEstado", "GuardadoPaso"
    [Required]
    [MaxLength(100)]
    public string Accion { get; set; } = string.Empty;

    public string? Detalle { get; set; }

    public DateTime FechaCambio { get; set; } = DateTime.UtcNow;

    public Expediente? Expediente { get; set; }
}
