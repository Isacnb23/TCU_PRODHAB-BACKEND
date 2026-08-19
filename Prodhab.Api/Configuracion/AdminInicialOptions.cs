namespace Prodhab.Api.Configuracion;

// Sección "AdminInicial" de appsettings.json.
// Si la BD no tiene ningún Usuario al arrancar, se crea un Admin con estos datos.
// En PRODUCCIÓN el Email/Password se inyectan por variables de entorno
// (AdminInicial__Email, AdminInicial__Password, AdminInicial__Nombre) y NUNCA se versionan en el repo.
public class AdminInicialOptions
{
    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
