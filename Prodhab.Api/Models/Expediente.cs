using System.ComponentModel.DataAnnotations;

namespace Prodhab.Api.Models;

public class Expediente
{
    public int Id { get; set; }

    // Nullable: PRODHAB lo asigna tras el primer envío. Formato ej: 001-01-2026-INS
    [MaxLength(50)]
    public string? NumeroExpediente { get; set; }

    [Required]
    [MaxLength(300)]
    public string Entidad { get; set; } = string.Empty;

    public int Anio { get; set; }

    public EstadoExpediente Estado { get; set; } = EstadoExpediente.Borrador;

    // Paso del wizard en el que quedó el expediente, para poder retomarlo.
    public int PasoActual { get; set; } = 1;

    public int UsuarioId { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;

    public DateTime? FechaEnvio { get; set; }

    public Usuario? Usuario { get; set; }

    public ICollection<DatosFormulario> Datos { get; set; } = new List<DatosFormulario>();

    public ICollection<Subsanacion> Subsanaciones { get; set; } = new List<Subsanacion>();

    public ICollection<HistorialExpediente> Historial { get; set; } = new List<HistorialExpediente>();
}
