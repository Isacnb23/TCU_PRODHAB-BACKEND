namespace Prodhab.Api.DTOs.Revision;

public class ObservacionDto
{
    public int Id { get; set; }

    public int Paso { get; set; }

    public string Texto { get; set; } = string.Empty;

    public string? Campo { get; set; }

    public DateTime FechaCreacion { get; set; }
}
