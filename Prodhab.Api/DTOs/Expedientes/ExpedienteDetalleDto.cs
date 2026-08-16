namespace Prodhab.Api.DTOs.Expedientes;

// Respuesta completa: lo que el wizard necesita para retomar un expediente.
public class ExpedienteDetalleDto
{
    public int Id { get; set; }

    public string? NumeroExpediente { get; set; }

    public string Entidad { get; set; } = string.Empty;

    public int Anio { get; set; }

    public string Estado { get; set; } = string.Empty;

    public int PasoActual { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public DateTime? FechaEnvio { get; set; }

    public List<DatosFormularioDto> Datos { get; set; } = new();
}
