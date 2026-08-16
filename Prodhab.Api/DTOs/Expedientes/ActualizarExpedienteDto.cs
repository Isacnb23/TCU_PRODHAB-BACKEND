using System.ComponentModel.DataAnnotations;

namespace Prodhab.Api.DTOs.Expedientes;

public class ActualizarExpedienteDto
{
    [Required]
    [MaxLength(300)]
    public string Entidad { get; set; } = string.Empty;

    [Range(2000, 2100)]
    public int Anio { get; set; }
}
