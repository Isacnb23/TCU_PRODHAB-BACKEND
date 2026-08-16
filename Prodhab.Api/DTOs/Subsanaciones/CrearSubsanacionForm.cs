using System.ComponentModel.DataAnnotations;

namespace Prodhab.Api.DTOs.Subsanaciones;

// Request multipart del POST de subsanación: se bindea con [FromForm], no es JSON.
public class CrearSubsanacionForm
{
    // Alineado con DatosFormulario.Paso: 1..9. El rango lo valida el servicio.
    public int Paso { get; set; }

    [Required]
    [MaxLength(200)]
    public string Campo { get; set; } = string.Empty;

    // Texto y archivo son opcionales por separado, pero al menos uno es obligatorio.
    public string? TextoJustificacion { get; set; }

    public IFormFile? Archivo { get; set; }
}
