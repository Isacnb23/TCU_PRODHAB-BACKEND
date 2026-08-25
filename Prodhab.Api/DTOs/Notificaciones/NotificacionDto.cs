namespace Prodhab.Api.DTOs.Notificaciones;

public class NotificacionDto
{
    public int Id { get; set; }

    public string Mensaje { get; set; } = string.Empty;

    public int? ExpedienteId { get; set; }

    public bool Leida { get; set; }

    public DateTime FechaCreacion { get; set; }
}
