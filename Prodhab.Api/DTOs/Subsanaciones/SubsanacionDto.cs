namespace Prodhab.Api.DTOs.Subsanaciones;

// Respuesta pública: sin ArchivoRuta, que es la ruta física interna.
public class SubsanacionDto
{
    public int Id { get; set; }

    public int Paso { get; set; }

    public string Campo { get; set; } = string.Empty;

    public string? TextoJustificacion { get; set; }

    public bool TieneArchivo { get; set; }

    // Nombre original del usuario, solo para mostrar y para la descarga.
    public string? ArchivoNombre { get; set; }

    public string? ArchivoMimeType { get; set; }

    public string? ArchivoExtension { get; set; }

    public long? ArchivoTamanoBytes { get; set; }

    public string? ArchivoHash { get; set; }

    public DateTime FechaSubsanacion { get; set; }
}
