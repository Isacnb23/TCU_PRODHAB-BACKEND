# Paso 3 — Guardado de pasos del wizard (Prodhab.Api)

Backend .NET 10 / EF Core / SQL Server (LocalDB). Ya existe:
- Capa de datos intacta (`/Models`, `/Data/AppDbContext`, migraciones).
- CRUD de Expedientes: `ExpedienteService`, `ExpedientesController`, DTOs en `/DTOs/Expedientes`,
  manejo de errores central (`NotFoundException`, `BusinessRuleException`, `GlobalExceptionHandler`),
  `ICurrentUserService`, `DbSeeder`.

## Alcance
Implementar el guardado/lectura de los **datos de cada paso del wizard** (entidad `DatosFormulario`),
que el front React llamará en cada paso. Arquitectura igual que el paso 2:
**Controller → Servicio → DbContext**, con DTOs, validación y errores centralizados.
NO tocar la capa de datos ni el CRUD de expedientes ya existente. NO implementar subsanaciones ni auth.

El wizard tiene 9 pasos. Cada paso guarda un JSON completo en una fila de DatosFormulario,
única por `(ExpedienteId, Paso)` (el índice único ya existe).

---

## 1. Validación de JSON — atributo reutilizable

Carpeta `/Infrastructure`, archivo `ValidJsonAttribute.cs` (namespace `Prodhab.Api.Infrastructure`):
- Hereda de `ValidationAttribute`.
- En `IsValid`: si el valor es string no vacío, intenta `System.Text.Json.JsonDocument.Parse`.
  Si parsea, válido; si lanza excepción, inválido con mensaje "El contenido del paso no es un JSON válido."
- string null/vacío → dejar que `[Required]` lo maneje (retornar ValidationResult.Success si es null).

---

## 2. DTO de entrada — `/DTOs/Expedientes/GuardarPasoDto.cs`

**GuardarPasoDto.cs** (request del PUT de paso)
- `string DatosJson` — `[Required]`, `[ValidJson]`
- `bool Completado`

La respuesta reutiliza el `DatosFormularioDto` que ya existe (Paso, DatosJson, Completado, FechaActualizacion).

---

## 3. Servicio — `/Services`

**IDatosFormularioService.cs**
```
Task<DatosFormularioDto> GuardarPasoAsync(int expedienteId, int paso, GuardarPasoDto dto, CancellationToken ct);
Task<DatosFormularioDto> ObtenerPasoAsync(int expedienteId, int paso, CancellationToken ct);
```

**DatosFormularioService.cs** — recibe `AppDbContext`. Constante `PasoMinimo = 1`, `PasoMaximo = 9`.

**GuardarPasoAsync** (UPSERT):
1. Validar `paso` en rango [1, 9]; si no → `BusinessRuleException($"El paso {paso} no es válido (debe estar entre 1 y 9).")`.
2. Cargar el Expediente por id; si no existe → `NotFoundException($"No existe el expediente {expedienteId}.")`.
3. Si `Estado != Borrador` → `BusinessRuleException("Solo se pueden editar los pasos de un expediente en estado Borrador.")`.
4. Buscar la fila DatosFormulario por `(ExpedienteId, Paso)`:
   - Si NO existe: crear una nueva.
   - Si existe: actualizarla.
   Setear `DatosJson = dto.DatosJson`, `FechaActualizacion = UtcNow`.
5. Detectar transición de completado: guardar si `Completado` pasa de false→true en esta operación
   (comparar el valor previo de la fila —si era nueva, previo = false— contra `dto.Completado`).
   Setear `Completado = dto.Completado`.
6. Avanzar el expediente: `Expediente.PasoActual = Math.Max(Expediente.PasoActual, paso)`,
   `Expediente.FechaModificacion = UtcNow`.
7. SOLO si hubo transición false→true en `Completado`, registrar en HistorialExpediente:
   Accion="PasoCompletado", Detalle=$"Paso {paso} completado", UsuarioId del expediente, FechaCambio=UtcNow.
   (No loguear guardados de rutina — evita inundar la bitácora con autosaves.)
8. `SaveChangesAsync`. Devolver el DatosFormularioDto de la fila guardada.

**ObtenerPasoAsync**:
- Validar rango de paso igual que arriba.
- Buscar la fila `(ExpedienteId, Paso)`. Si el expediente no existe → NotFoundException.
  Si el expediente existe pero el paso aún no se guardó → NotFoundException
  ($"El paso {paso} del expediente {expedienteId} aún no tiene datos guardados.").
- Devolver DatosFormularioDto (proyección inline, sin métodos de mapeo dentro del IQueryable).

Registrar en Program.cs:
```csharp
builder.Services.AddScoped<IDatosFormularioService, DatosFormularioService>();
```

---

## 4. Controller

Agregar los endpoints anidados bajo expedientes. Podés extender `ExpedientesController` o crear
`PasosController` con ruta `[Route("api/expedientes/{expedienteId:int}/pasos")]`.
Preferí un controller separado `PasosController` para mantener el de expedientes enfocado.
Inyecta `IDatosFormularioService`. Métodos async con CancellationToken y `[ProducesResponseType]`.

- **PUT `/api/expedientes/{expedienteId:int}/pasos/{paso:int}`**
  - body `GuardarPasoDto` → `200 OK` con el `DatosFormularioDto` guardado.
  - (400 validación, 404 expediente inexistente, 409 no-Borrador o paso fuera de rango: los maneja el handler global.)

- **GET `/api/expedientes/{expedienteId:int}/pasos/{paso:int}`**
  - → `200` DatosFormularioDto | 404.

---

## Al terminar
- `dotnet build` limpio.
- Levantar la API y probar el flujo real (no mental):
  1. POST crear expediente (Borrador) → obtener id.
  2. PUT paso 1 con un JSON válido, completado=false → 200, GET del expediente muestra datos[0] y pasoActual=1.
  3. PUT paso 1 de nuevo (mismo paso, JSON distinto, completado=true) → 200, sigue habiendo UNA sola fila
     del paso 1 (upsert, no duplicado), y ahora hay una entrada "PasoCompletado" en HistorialExpediente.
  4. PUT paso 3 → pasoActual salta a 3.
  5. PUT con DatosJson = "{no es json" → 400.
  6. PUT paso 15 → 409 (fuera de rango).
  7. Forzar el expediente a Enviado y PUT cualquier paso → 409 (no-Borrador).
  8. GET de un paso no guardado → 404.
- Reportar resultados, el árbol de archivos nuevos y confirmar que el índice único evitó duplicados en el upsert.
- Limpiar los datos de prueba al final (dejar el usuario de desarrollo).
