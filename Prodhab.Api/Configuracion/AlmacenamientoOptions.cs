namespace Prodhab.Api.Configuracion;

// Sección "Almacenamiento" de appsettings.json.
public class AlmacenamientoOptions
{
    // Relativa al ContentRootPath: nunca se expone como carpeta de archivos estáticos.
    public string RutaBase { get; set; } = "Storage/Subsanaciones";

    public long TamanoMaximoBytes { get; set; } = 10 * 1024 * 1024;
}
