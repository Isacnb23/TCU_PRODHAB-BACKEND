# Paso 4 — Subsanaciones + upload de archivos con validación real (Prodhab.Api)

Backend .NET 10 / EF Core / SQL Server (LocalDB). Ya existe y funciona:
- Capa de datos intacta (`/Models`, `/Data/AppDbContext`, migraciones). La entidad `Subsanacion`
  ya tiene TODOS los campos necesarios: TextoJustificacion, ArchivoRuta, ArchivoNombre,
  ArchivoMimeType, ArchivoExtension, ArchivoTamanoBytes, ArchivoHash, Campo, Paso, ExpedienteId,
  UsuarioId, FechaCreacion, FechaSubsanacion. El CHECK constraint
  `CK_Subsanacion_JustificacionOArchivo` ya existe en la BD.
- CRUD de Expedientes, guardado de pasos, manejo de errores central
  (`NotFoundException`, `BusinessRuleException`, `GlobalExceptionHandler`), `ICurrentUserService`,
  `DbSeeder`. Arquitectura Controller → Servicio → DbContext.

## Alcance
Implementar el módulo de **Subsanaciones** con carga de archivos validados por CONTENIDO REAL
(no por extensión). NO tocar la capa de datos, el CRUD de expedientes ni el guardado de pasos.
NO implementar auth (usar `ICurrentUserService` como hasta ahora).

Reglas de arquitectura: mantener el patrón en capas. Sin repositorio ni MediatR.

---

## 1. Configuración de almacenamiento

En `appsettings.json`, agregar una sección:
```json
"Almacenamiento": {
  "RutaBase": "Storage/Subsanaciones",
  "TamanoMaximoBytes": 10485760
}
```
(10485760 = 10 MB.)

Crear `/Configuracion/AlmacenamientoOptions.cs` (namespace `Prodhab.Api.Configuracion`):
- `string RutaBase`
- `long TamanoMaximoBytes`

Enlazar con Options pattern en Program.cs:
```csharp
builder.Services.Configure<AlmacenamientoOptions>(
    builder.Configuration.GetSection("Almacenamiento"));
```

Agregar `Storage/` al `.gitignore` (los archivos subidos no van al repo).
NO configurar la carpeta Storage como archivos estáticos (no debe ser accesible por URL directa).

---

## 2. Validador de archivos por contenido — `/Services`

**ResultadoValidacionArchivo.cs** (record o clase, namespace `Prodhab.Api.Services`):
- `bool EsValido`
- `string? Error`
- `string? MimeDetectado`
- `string? Extension`   // normalizada, sin punto, minúscula: "pdf" | "docx"

**IValidadorArchivo.cs**
```
Task<ResultadoValidacionArchivo> ValidarAsync(IFormFile archivo, CancellationToken ct);
```

**ValidadorArchivo.cs** — recibe `IOptions<AlmacenamientoOptions>`. Lógica EN CAPAS:

Extensiones permitidas: solo `.pdf` y `.docx` (case-insensitive).
Tipos MIME reales resultantes:
- pdf  → `application/pdf`
- docx → `application/vnd.openxmlformats-officedocument.wordprocessingml.document`

Pasos de validación (si alguno falla, devolver EsValido=false con Error descriptivo en español):
1. Archivo no nulo y `Length > 0` → si vacío: "El archivo está vacío."
2. `Length <= TamanoMaximoBytes` → si excede: "El archivo supera el tamaño máximo permitido (10 MB)."
3. Extensión (de `archivo.FileName`) en el allowlist → si no: "Solo se permiten archivos PDF o Word (.docx)."
4. Leer los primeros bytes del stream y validar MAGIC BYTES por contenido (NO confiar en
   `archivo.ContentType`, que lo manda el navegador):
   - **PDF**: los primeros 5 bytes deben ser `%PDF-` (0x25 0x50 0x44 0x46 0x2D).
     Si la extensión es .pdf pero no arranca con %PDF- → "El archivo no es un PDF válido."
   - **DOCX**: los primeros 4 bytes deben ser la firma ZIP `PK\x03\x04` (0x50 0x4B 0x03 0x04).
     ADEMÁS, abrir el contenido como `System.IO.Compression.ZipArchive` (modo lectura) y
     confirmar que existe una entrada `word/document.xml`. Si no arranca como ZIP o no contiene
     esa entrada → "El archivo no es un documento Word (.docx) válido."
5. Coherencia extensión ↔ contenido: si la extensión dice .pdf el contenido debe ser PDF, y si
   dice .docx debe ser DOCX. Un archivo con extensión cambiada NO debe pasar.

IMPORTANTE sobre el stream: leer el IFormFile en un `MemoryStream` (está acotado a 10 MB por el
paso 2, así que bufferizar en memoria es seguro) para poder inspeccionar magic bytes y abrir el
zip sin consumir el stream que luego usa el almacenamiento. Resetear posición (`Position = 0`)
donde haga falta.

---

## 3. Servicio de almacenamiento en disco — `/Services`

**ArchivoGuardado.cs** (record, namespace `Prodhab.Api.Services`):
- `string RutaRelativa`     // lo que va en ArchivoRuta (ej: "Storage/Subsanaciones/{guid}.pdf")
- `string NombreFisico`     // "{guid}.pdf"
- `long TamanoBytes`
- `string HashSha256`       // hex, 64 chars

**IFileStorageService.cs**
```
Task<ArchivoGuardado> GuardarAsync(IFormFile archivo, string extension, CancellationToken ct);
Task EliminarAsync(string rutaRelativa, CancellationToken ct);
(string rutaFisica, bool existe) ResolverRutaFisica(string rutaRelativa);
```

**FileStorageService.cs** — recibe `IOptions<AlmacenamientoOptions>` y `IWebHostEnvironment` (o
`IHostEnvironment`) para resolver la ruta base absoluta contra `ContentRootPath`.

- **GuardarAsync**:
  - Generar nombre físico = `{Guid.NewGuid():N}.{extension}`.
  - Asegurar que la carpeta base exista (`Directory.CreateDirectory`).
  - Bufferizar el archivo, calcular SHA256 (hex minúscula) y tamaño.
  - Escribir los bytes a disco.
  - Devolver `ArchivoGuardado` con la ruta relativa (RutaBase + nombre físico), tamaño y hash.
- **EliminarAsync**: resolver la ruta física y borrar el archivo si existe (no fallar si ya no está).
- **ResolverRutaFisica**: combinar ContentRootPath + rutaRelativa. DEFENSA CONTRA PATH TRAVERSAL:
  verificar que la ruta física resuelta (`Path.GetFullPath`) siga estando DENTRO de la carpeta base
  absoluta; si no, tratar como inexistente. Devolver (rutaFisica, existe).

Registrar en Program.cs:
```csharp
builder.Services.AddScoped<IValidadorArchivo, ValidadorArchivo>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<ISubsanacionService, SubsanacionService>();
```

---

## 4. DTOs — `/DTOs/Subsanaciones` (namespace `Prodhab.Api.DTOs.Subsanaciones`)

**CrearSubsanacionForm.cs** (modelo de formulario multipart — se bindea con [FromForm]):
- `int Paso`                    // 1..9
- `string Campo`                // [Required], [MaxLength(200)]
- `string? TextoJustificacion`  // opcional
- `IFormFile? Archivo`          // opcional
(NO poner [ValidJson] ni nada de JSON acá; es multipart.)

**SubsanacionDto.cs** (response — NO exponer ArchivoRuta, es la ruta física interna):
- `int Id`
- `int Paso`
- `string Campo`
- `string? TextoJustificacion`
- `bool TieneArchivo`
- `string? ArchivoNombre`       // nombre original del usuario
- `string? ArchivoMimeType`
- `string? ArchivoExtension`
- `long? ArchivoTamanoBytes`
- `string? ArchivoHash`
- `DateTime FechaSubsanacion`

---

## 5. Servicio de Subsanaciones — `/Services`

**ISubsanacionService.cs**
```
Task<SubsanacionDto> CrearAsync(int expedienteId, CrearSubsanacionForm form, CancellationToken ct);
Task<List<SubsanacionDto>> ListarPorExpedienteAsync(int expedienteId, CancellationToken ct);
Task<SubsanacionDto> ObtenerPorIdAsync(int expedienteId, int subsanacionId, CancellationToken ct);
Task<(string rutaFisica, string nombreOriginal, string mimeType)> ObtenerArchivoAsync(int expedienteId, int subsanacionId, CancellationToken ct);
Task EliminarAsync(int expedienteId, int subsanacionId, CancellationToken ct);
```

**SubsanacionService.cs** — recibe `AppDbContext`, `IValidadorArchivo`, `IFileStorageService`,
`ICurrentUserService`.

**CrearAsync**:
1. Validar `Paso` en [1,9]; si no → BusinessRuleException.
2. Cargar Expediente; si no existe → NotFoundException.
3. Si `Estado == Aprobado` → BusinessRuleException("No se pueden registrar subsanaciones en un
   expediente ya aprobado.").
4. Regla justificación-o-archivo (espejo del CHECK, para dar 400 amigable):
   - hay texto = `!string.IsNullOrWhiteSpace(form.TextoJustificacion)`
   - hay archivo = `form.Archivo != null && form.Archivo.Length > 0`
   - si NO hay ninguno → BusinessRuleException("Debe proporcionar una justificación de texto o
     adjuntar un archivo para la subsanación.").
5. Si hay archivo: validarlo con `IValidadorArchivo`. Si `!EsValido` → BusinessRuleException(resultado.Error).
   Luego guardarlo con `IFileStorageService` (usando la extensión normalizada del validador).
6. Crear la entidad Subsanacion: ExpedienteId, Paso, Campo, TextoJustificacion (trim o null),
   UsuarioId del usuario actual, FechaCreacion = FechaSubsanacion = UtcNow. Si hubo archivo,
   completar ArchivoRuta, ArchivoNombre (= form.Archivo.FileName original), ArchivoMimeType
   (= MimeDetectado del validador, NO el del navegador), ArchivoExtension, ArchivoTamanoBytes,
   ArchivoHash.
7. Registrar en HistorialExpediente: Accion="Subsanacion",
   Detalle=$"Subsanación registrada — paso {Paso}, campo '{Campo}'", UsuarioId actual, UtcNow.
8. SaveChangesAsync. Devolver SubsanacionDto.
   (Si SaveChanges falla DESPUÉS de haber escrito el archivo, borrar el archivo del disco para no
   dejar huérfanos. Envolver en try/catch alrededor del guardado en BD.)

**ListarPorExpedienteAsync**: verificar que el expediente existe (NotFound si no). Devolver las
subsanaciones del expediente ordenadas por FechaSubsanacion desc, proyección inline a SubsanacionDto
(TieneArchivo = ArchivoRuta != null). No exponer ArchivoRuta.

**ObtenerPorIdAsync**: buscar por (ExpedienteId, Id); NotFound si no existe. Devolver SubsanacionDto.

**ObtenerArchivoAsync**: buscar la subsanación (NotFound si no existe). Si no tiene archivo
(ArchivoRuta null) → NotFoundException("Esta subsanación no tiene un archivo adjunto."). Resolver
la ruta física con `IFileStorageService.ResolverRutaFisica`; si el archivo no existe en disco →
NotFoundException("El archivo no se encontró en el almacenamiento."). Devolver (rutaFisica,
ArchivoNombre, ArchivoMimeType).

**EliminarAsync**: buscar (ExpedienteId, Id); NotFound si no existe. Si Expediente.Estado == Aprobado
→ BusinessRuleException. Borrar el archivo del disco (si tiene) con `IFileStorageService.EliminarAsync`.
Eliminar la entidad. Registrar HistorialExpediente Accion="SubsanacionEliminada". SaveChanges.

---

## 6. Controller — `/Controllers/SubsanacionesController.cs`

`[ApiController]`, `[Route("api/expedientes/{expedienteId:int}/subsanaciones")]`.
Inyecta `ISubsanacionService`. Métodos async con CancellationToken y `[ProducesResponseType]`.

- **POST `""`** — `[Consumes("multipart/form-data")]`, parámetro `[FromForm] CrearSubsanacionForm form`.
  Agregar `[RequestSizeLimit(12_000_000)]` (10 MB + margen para el resto del multipart).
  → `201 Created` con `CreatedAtAction` al GET por id, cuerpo = SubsanacionDto.
- **GET `""`** — → `200` List<SubsanacionDto>.
- **GET `"{subsanacionId:int}"`** — → `200` SubsanacionDto | 404.
- **GET `"{subsanacionId:int}/archivo"`** — llama a `ObtenerArchivoAsync` y devuelve el archivo
  físico con `PhysicalFile(rutaFisica, mimeType, nombreOriginal)` (esto setea Content-Disposition
  con el nombre original para la descarga). El content-type es el VALIDADO, no el del navegador.
  → descarga | 404.
- **DELETE `"{subsanacionId:int}"`** — → `204 No Content` | 404 | 409.

---

## 7. Límite de tamaño de multipart (global, por si acaso)

En Program.cs, configurar el límite del body multipart acorde:
```csharp
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 12_000_000;
});
```

---

## Al terminar
- `dotnet build` limpio.
- Levantar la API y probar el flujo REAL (no mental). Preparar archivos de prueba en disco:
  1. Crear un expediente (POST) → id.
  2. POST subsanación SOLO con texto (sin archivo) → 201, TieneArchivo=false.
  3. POST subsanación con un PDF real válido → 201, TieneArchivo=true, hash y tamaño presentes,
     ArchivoMimeType = application/pdf.
  4. POST subsanación con un .docx real válido → 201, mime docx correcto.
  5. POST SIN texto y SIN archivo → 400/409 con el mensaje de justificación-o-archivo.
  6. Tomar un .txt (o un .png) y renombrarlo a .pdf → POST → rechazado ("no es un PDF válido").
     Esta es LA prueba clave: la validación por contenido, no por extensión.
  7. Renombrar un .zip cualquiera (que NO sea docx) a .docx → POST → rechazado
     ("no es un documento Word válido") porque no contiene word/document.xml.
  8. Subir un archivo > 10 MB → 400/409 por tamaño.
  9. GET lista → muestra todas las subsanaciones del expediente (sin ArchivoRuta).
  10. GET /{id}/archivo de una con PDF → descarga el archivo con el nombre ORIGINAL.
  11. DELETE una subsanación con archivo → 204, y confirmar que el archivo se BORRÓ del disco
      (no quedó huérfano en Storage/).
  12. Forzar expediente a Aprobado + POST subsanación → 409.
- Confirmar en disco que los archivos se guardan con nombre GUID (no el original) en Storage/Subsanaciones.
- Reportar resultados, árbol de archivos nuevos, y limpiar los datos de prueba (borrar expedientes de
  prueba y sus archivos; dejar el usuario de desarrollo).
