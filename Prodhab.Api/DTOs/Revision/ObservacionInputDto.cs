using System.ComponentModel.DataAnnotations;

namespace Prodhab.Api.DTOs.Revision;

public class ObservacionInputDto
{
    [Range(1, 9)]
    public int Paso { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Texto { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Campo { get; set; }
}
