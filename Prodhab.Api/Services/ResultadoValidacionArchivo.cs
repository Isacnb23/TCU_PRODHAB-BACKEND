namespace Prodhab.Api.Services;

// Resultado de validar un archivo subido por CONTENIDO, no por extensión.
public record ResultadoValidacionArchivo
{
    public bool EsValido { get; init; }

    // Mensaje en español, listo para devolver al cliente.
    public string? Error { get; init; }

    // MIME deducido del contenido real, no el que reportó el navegador.
    public string? MimeDetectado { get; init; }

    // Normalizada: sin punto y en minúscula ("pdf" | "docx").
    public string? Extension { get; init; }

    public static ResultadoValidacionArchivo Invalido(string error) =>
        new() { EsValido = false, Error = error };

    public static ResultadoValidacionArchivo Valido(string mimeDetectado, string extension) =>
        new() { EsValido = true, MimeDetectado = mimeDetectado, Extension = extension };
}
