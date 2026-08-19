using System.ComponentModel.DataAnnotations;

namespace Prodhab.Api.Models;

public class Observacion
{
    public int Id { get; set; }

    public int ExpedienteId { get; set; }

    public int Paso { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Texto { get; set; } = string.Empty;

    // Admin que la creó. Columna simple, sin navegación, para evitar cascade paths.
    public int UsuarioId { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public Expediente? Expediente { get; set; }
}
