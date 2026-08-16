# Paso 2 — CRUD de Expedientes (Prodhab.Api)

Backend .NET 10 / EF Core / SQL Server (LocalDB). La capa de datos ya existe:
`AppDbContext` en `/Data`, entidades en `/Models` (Expediente, DatosFormulario, Subsanacion,
Usuario, HistorialExpediente, enum EstadoExpediente). Migración inicial ya aplicada.

## Alcance
Implementar el CRUD de **Expedientes** con arquitectura profesional en capas:
**Controller → Servicio → DbContext**, con DTOs, validación, manejo de errores centralizado
y async en todo. NO tocar la capa de datos (`/Models`, `/Data`, migraciones). NO implementar
todavía guardado de pasos, subsanaciones ni auth.

Regla de arquitectura: NO agregar patrón repositorio ni MediatR. El DbContext ya cumple ese rol.
Mantenerlo simple y mantenible.

---

## 1. DTOs — carpeta `/DTOs/Expedientes` (namespace `Prodhab.Api.DTOs.Expedientes`)

**CrearExpedienteDto.cs** (request POST)
- `string Entidad` — `[Required]`, `[MaxLength(300)]`
- `int Anio` — `[Range(2000, 2100)]`

**ActualizarExpedienteDto.cs** (request PUT — solo cabecera)
- `string Entidad` — `[Required]`, `[MaxLength(300)]`
- `int Anio` — `[Range(2000, 2100)]`

**ExpedienteListaDto.cs** (response de la grilla — SIN el JSON de los pasos)
- `int Id`
- `string? NumeroExpediente`
- `string Entidad`
- `int Anio`
- `string Estado` (el enum como string)
- `int PasoActual`
- `DateTime FechaCreacion`
- `DateTime FechaModificacion`

**ExpedienteDetalleDto.cs** (response completo para retomar el wizard)
- todo lo de ExpedienteListaDto
- `DateTime? FechaEnvio`
- `List<DatosFormularioDto> Datos`

**DatosFormularioDto.cs** (anidado en el detalle)
- `int Paso`
- `string DatosJson`
- `bool Completado`
- `DateTime FechaActualizacion`

---

## 2. Manejo de errores centralizado

Carpeta `/Exceptions` (namespace `Prodhab.Api.Exceptions`):
- **NotFoundException.cs** : Exception — para 404
- **BusinessRuleException.cs** : Exception — para 409 (regla de negocio violada)

Carpeta `/Infrastructure` (namespace `Prodhab.Api.Infrastructure`):
- **GlobalExceptionHandler.cs** implementando `IExceptionHandler`:
  - `NotFoundException` → 404 con ProblemDetails (title "Recurso no encontrado")
  - `BusinessRuleException` → 409 con ProblemDetails (title "Operación no permitida")
  - cualquier otra → 500 genérico (sin filtrar el mensaje interno). Loguear con ILogger.
  - Usar el `message` de la excepción como `detail` del ProblemDetails en los casos 404/409.

En `Program.cs`:
- `builder.Services.AddProblemDetails();`
- `builder.Services.AddExceptionHandler<GlobalExceptionHandler>();`
- `app.UseExceptionHandler();`

---

## 3. Usuario actual (puente temporal, desacoplado de auth)

Carpeta `/Services` (namespace `Prodhab.Api.Services`):

**ICurrentUserService.cs**
- `int GetUserId();`

**CurrentUserService.cs** (implementación de DESARROLLO — dejar un comentario `// TODO: reemplazar por lectura del claim JWT en Paso 5`)
- Recibe `AppDbContext` por constructor.
- `GetUserId()` devuelve el `Id` del primer usuario activo de la BD (el usuario de desarrollo sembrado abajo). Cachear el valor tras la primera consulta.

**Seeder de desarrollo** — carpeta `/Data`, archivo `DbSeeder.cs`:
- Método estático `SeedDevAsync(AppDbContext db)`:
  - si NO existe ningún Usuario, crea uno: Nombre "Usuario de Desarrollo",
    Email "dev@prodhab.local", Rol "Admin", Activo true, PasswordHash con BCrypt de "dev123".
  - Requiere el paquete `BCrypt.Net-Next` (instalarlo).
- En `Program.cs`, SOLO en desarrollo, ejecutar el seeder al arrancar:
  ```csharp
  if (app.Environment.IsDevelopment())
  {
      using var scope = app.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
      await Prodhab.Api.Data.DbSeeder.SeedDevAsync(db);
  }
  ```

---

## 4. Servicio de Expedientes — `/Services`

**IExpedienteService.cs**
```
Task<ExpedienteDetalleDto> CrearAsync(CrearExpedienteDto dto, CancellationToken ct);
Task<List<ExpedienteListaDto>> ListarPorUsuarioAsync(int usuarioId, CancellationToken ct);
Task<ExpedienteDetalleDto> ObtenerPorIdAsync(int id, CancellationToken ct);
Task ActualizarAsync(int id, ActualizarExpedienteDto dto, CancellationToken ct);
Task EliminarAsync(int id, CancellationToken ct);
```

**ExpedienteService.cs** — recibe `AppDbContext` y `ICurrentUserService`. Reglas:

- **CrearAsync**: crea Expediente con Estado=Borrador, PasoActual=1, NumeroExpediente=null,
  UsuarioId = current user, FechaCreacion/FechaModificacion = UtcNow. Guarda una fila en
  HistorialExpediente (Accion="Creacion", Detalle="Expediente creado en borrador").
  Devuelve el ExpedienteDetalleDto (el wizard necesita el Id).

- **ListarPorUsuarioAsync**: devuelve los expedientes del usuario ordenados por
  FechaModificacion desc. Proyección INLINE a ExpedienteListaDto dentro del Select
  (traducible a SQL). NO cargar los DatosFormulario acá.

- **ObtenerPorIdAsync**: incluye los DatosFormulario (Include o proyección inline).
  Si no existe, lanza NotFoundException($"No existe el expediente {id}.").

- **ActualizarAsync**: solo permite modificar si Estado == Borrador; si no, lanza
  BusinessRuleException("Solo se pueden modificar expedientes en estado Borrador.").
  Si no existe → NotFoundException. Actualiza Entidad/Anio y FechaModificacion=UtcNow.
  Registra HistorialExpediente (Accion="Actualizacion").

- **EliminarAsync**: solo si Estado == Borrador; si no, BusinessRuleException("No se puede
  eliminar un expediente que ya fue enviado a PRODHAB."). Si no existe → NotFoundException.
  Elimina (cascade se encarga de Datos/Subsanaciones/Historial).

⚠ MAPEO: usar proyecciones inline (`new ExpedienteListaDto { ... }`) traducibles a SQL.
NUNCA llamar un método de mapeo dentro de una consulta IQueryable — eso falla en runtime en
EF Core. Si necesitás mapear tras materializar, hacelo después del ToListAsync/FirstAsync.

Registrar en Program.cs:
```csharp
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IExpedienteService, ExpedienteService>();
```

---

## 5. Controller — `/Controllers/ExpedientesController.cs`

`[ApiController]`, `[Route("api/expedientes")]`. Inyecta `IExpedienteService`.
Controller FLACO: sin lógica de negocio, solo orquesta y devuelve el status correcto.
Todos los métodos async con CancellationToken. Decorar con `[ProducesResponseType]` para OpenAPI.

- **POST `/api/expedientes`** — body `CrearExpedienteDto` → `201 Created` con
  `CreatedAtAction` apuntando al GET por id, cuerpo = ExpedienteDetalleDto.
- **GET `/api/expedientes`** — usa el usuario actual (`ICurrentUserService`) → `200` List<ExpedienteListaDto>.
- **GET `/api/expedientes/{id}`** — → `200` ExpedienteDetalleDto (404 lo maneja el handler global).
- **PUT `/api/expedientes/{id}`** — body `ActualizarExpedienteDto` → `204 No Content`.
- **DELETE `/api/expedientes/{id}`** — → `204 No Content`.

Con `[ApiController]`, la validación de DataAnnotations devuelve 400 ValidationProblemDetails
automáticamente. No agregar validación manual redundante.

---

## 6. CORS (para que el front React/Vite conecte)

En Program.cs, agregar una política CORS de desarrollo que permita el origen de Vite
(`http://localhost:5173`), con AllowAnyHeader y AllowAnyMethod. Aplicarla con `app.UseCors(...)`.

---

## Al terminar
- `dotnet build` limpio.
- Mostrar el árbol de `/DTOs`, `/Services`, `/Exceptions`, `/Infrastructure`, `/Controllers`.
- Confirmar que el seeder crea el usuario de desarrollo al arrancar.
- Probar mentalmente el flujo: POST crea borrador y devuelve Id → GET lista lo muestra →
  PUT lo edita → DELETE lo borra. Reportar los endpoints listos para probar en Thunder Client.
