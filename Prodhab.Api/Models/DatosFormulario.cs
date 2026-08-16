namespace Prodhab.Api.Models;

public class DatosFormulario
{
    public int Id { get; set; }

    public int ExpedienteId { get; set; }

    // 1..9
    public int Paso { get; set; }

    // JSON completo del paso.
    public string DatosJson { get; set; } = "{}";

    public bool Completado { get; set; }

    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;

    public Expediente? Expediente { get; set; }
}
