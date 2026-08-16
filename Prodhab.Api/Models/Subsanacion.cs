using System.ComponentModel.DataAnnotations;

namespace Prodhab.Api.Models;

public class Subsanacion
{
    public int Id { get; set; }

    public int ExpedienteId { get; set; }

    // Alineado con DatosFormulario.Paso (no texto libre).
    public int Paso { get; set; }

    [Required]
    [MaxLength(200)]
    public string Campo { get; set; } = string.Empty;

    public string? TextoJustificacion { get; set; }

    // GUID con el que se guarda el archivo en disco.
    public string? ArchivoRuta { get; set; }

    // Nombre original del usuario: nunca usarlo como nombre físico.
    [MaxLength(300)]
    public string? ArchivoNombre { get; set; }

    // MIME real validado por buffer, no el que reporta el navegador.
    [MaxLength(150)]
    public string? ArchivoMimeType { get; set; }

    [MaxLength(10)]
    public string? ArchivoExtension { get; set; }

    public long? ArchivoTamanoBytes { get; set; }

    // SHA256
    [MaxLength(64)]
    public string? ArchivoHash { get; set; }

    public int UsuarioId { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public DateTime FechaSubsanacion { get; set; } = DateTime.UtcNow;

    public Expediente? Expediente { get; set; }
}
