using System.ComponentModel.DataAnnotations;

namespace Prodhab.Api.DTOs.Revision;

public class SolicitarSubsanacionDto
{
    [Required]
    [MinLength(1)]
    public List<ObservacionInputDto> Observaciones { get; set; } = new();
}
