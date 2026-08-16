namespace Prodhab.Api.Configuracion;

// Sección "Jwt" de appsettings.json.
// En PRODUCCIÓN la Key se inyecta por variable de entorno Jwt__Key y NUNCA se versiona en el repo.
public class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    // HMAC-SHA256 exige al menos 32 caracteres (256 bits).
    public string Key { get; set; } = string.Empty;

    public int ExpiryHours { get; set; } = 8;
}
