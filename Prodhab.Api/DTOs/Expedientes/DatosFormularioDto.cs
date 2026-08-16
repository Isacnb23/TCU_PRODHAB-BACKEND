namespace Prodhab.Api.DTOs.Expedientes;

public class DatosFormularioDto
{
    public int Paso { get; set; }

    public string DatosJson { get; set; } = "{}";

    public bool Completado { get; set; }

    public DateTime FechaActualizacion { get; set; }
}
