using System.ComponentModel.DataAnnotations;
using Prodhab.Api.Infrastructure;

namespace Prodhab.Api.DTOs.Expedientes;

// Request del PUT de un paso del wizard. La respuesta reutiliza DatosFormularioDto.
public class GuardarPasoDto
{
    [Required]
    [ValidJson]
    public string DatosJson { get; set; } = string.Empty;

    public bool Completado { get; set; }
}
