namespace Prodhab.Api.Services;

// Datos del archivo ya escrito en disco, para persistir en la entidad Subsanacion.
public record ArchivoGuardado(
    // Lo que va en ArchivoRuta: relativo al ContentRootPath (ej: "Storage/Subsanaciones/{guid}.pdf").
    string RutaRelativa,
    // Nombre físico GUID: nunca el que mandó el usuario.
    string NombreFisico,
    long TamanoBytes,
    // SHA256 en hex minúscula (64 chars).
    string HashSha256);
