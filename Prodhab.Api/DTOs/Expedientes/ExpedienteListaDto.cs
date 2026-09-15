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

    // Dueño del expediente. Solo tiene sentido mostrarlo cuando quien lista es Admin
    // (ve expedientes de todos); para un Usuario normal siempre es él mismo. UsuarioId
    // también le sirve al frontend para decidir si el propio Admin puede seguir
    // editando en el wizard un Borrador/RequiereSubsanacion suyo, o si es de otro y
    // solo corresponde revisarlo (solo lectura salvo que esté Enviado).
    public int UsuarioId { get; set; }

    public string UsuarioNombre { get; set; } = string.Empty;

    public string UsuarioEmail { get; set; } = string.Empty;

    // Señal derivada: true si el expediente ya tuvo al menos una observación
    // (nunca se borran), es decir, si un envío en estado Enviado es en
    // realidad un reenvío tras subsanación.
    public bool TieneObservacionesPrevias { get; set; }
}
