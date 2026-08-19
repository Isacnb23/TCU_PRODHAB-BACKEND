using System.ComponentModel.DataAnnotations;

namespace Prodhab.Api.DTOs.Revision;

public class AprobarDto
{
    [Required]
    [MaxLength(50)]
    public string NumeroExpediente { get; set; } = string.Empty;
}
