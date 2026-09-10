namespace Prodhab.Api.DTOs.Expedientes;

// Respuesta de la grilla: sin el JSON de los pasos.
public class ExpedienteListaDto
{
    public int Id { get; set; }

    public string? NumeroExpediente { get; set; }

    public string Entidad { get; set; } = string.Empty;

    public int Anio { get; set; }

    public string Estado { get; set; } = string.Empty;

    public int PasoActual { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    // Señal derivada: true si el expediente ya tuvo al menos una observación
    // (nunca se borran), es decir, si un envío en estado Enviado es en
    // realidad un reenvío tras subsanación.
    public bool TieneObservacionesPrevias { get; set; }
}
