using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Prodhab.Api.Infrastructure;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class ValidJsonAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // Null o vacío no es asunto de este atributo: lo cubre [Required].
        if (value is not string contenido || string.IsNullOrWhiteSpace(contenido))
        {
            return ValidationResult.Success;
        }

        try
        {
            using var _ = JsonDocument.Parse(contenido);
            return ValidationResult.Success;
        }
        catch (JsonException)
        {
            return new ValidationResult("El contenido del paso no es un JSON válido.");
        }
    }
}
