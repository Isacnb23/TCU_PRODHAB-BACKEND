# Subsanación por campo — Backend (Prodhab.Api)

Backend .NET 10 / EF Core / SQL Server, completo y probado. La entidad `Observacion` existe
(Id, ExpedienteId, Paso, Texto, UsuarioId, FechaCreacion) — hoy es UNA observación por PASO.
La entidad `Subsanacion` YA tiene un campo `Campo` (string) desde su diseño original — el Usuario
ya adjunta subsanaciones asociadas a un campo específico dentro del paso, eso no cambia.

## Objetivo
Migrar `Observacion` para que sea por CAMPO específico dentro de un paso, no por paso entero.
Esto alinea `Observacion` con `Subsanacion`, que ya funciona así. NO cambiar el comportamiento de
Aprobar, ni el estado del expediente, ni ningún otro endpoint. Es un cambio aditivo y acotado.

---

## 1. Migración de esquema

Agregar columna `Campo` (string, nullable, max 200) a `Observacion` en `/Models/Observacion.cs` y
en `AppDbContext`. Nullable para no romper filas históricas que pudieran existir sin campo (aunque
en la práctica la tabla puede estar vacía tras la limpieza de datos de prueba — igual usar nullable
por seguridad, no asumir que está vacía).

Generar y aplicar migración: `dotnet ef migrations add AgregarCampoAObservacion` +
`dotnet ef database update`. Verificar que el script SOLO agrega la columna (ALTER TABLE ADD),
sin tocar otras tablas.

---

## 2. DTOs

En `ObservacionInputDto.cs` (usado al solicitar subsanación): agregar
`string? Campo` (opcional a nivel de anotación para no romper si algún caller viejo no lo manda,
pero validar en el SERVICIO que venga no vacío — ver punto 3).

En `ObservacionDto.cs` (salida): agregar `string? Campo`.

---

## 3. Servicio — `RevisionService.SolicitarSubsanacionAsync`

- Validar que CADA observación del DTO tenga `Campo` no vacío (además del `Paso` en [1,9] que ya se
  valida). Si falta → `ValidationException("Cada observación debe indicar a qué campo corresponde.")`.
- Al crear las filas `Observacion`, persistir también `Campo`.
- El resto de la lógica (estado debe ser Enviado, historial, etc.) NO cambia.

En `ExpedienteService.ObtenerPorIdAsync` (donde se puebla `ExpedienteDetalleDto.Observaciones`):
incluir `Campo` en la proyección de `ObservacionDto`.

---

## Al terminar
- `dotnet build` limpio. Migración aplicada, verificada como aditiva (solo ALTER TABLE ADD COLUMN).
- Probar el flujo real (login admin, crear+completar+enviar un expediente):
  1. POST `/solicitar-subsanacion` con `observaciones: [{ paso: 3, campo: "nombreBaseDatos", texto: "..." }]`
     → 200, y el GET del detalle trae esa observación con su `campo`.
  2. POST `/solicitar-subsanacion` con una observación SIN `campo` → 400 (ValidationException).
  3. Confirmar que Aprobar y el resto de endpoints no cambiaron de comportamiento.
- Reportar resultados y confirmar que ningún otro endpoint fue afectado. Limpiar datos de prueba
  (dejar el usuario dev).
