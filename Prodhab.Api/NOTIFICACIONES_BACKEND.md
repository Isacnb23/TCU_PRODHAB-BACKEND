# Notificaciones del sistema — Backend (Prodhab.Api)

Backend .NET 10 / EF Core / SQL Server, completo y probado. Puntos de disparo ya existentes y
probados: `RevisionService.SolicitarSubsanacionAsync`, `RevisionService.AprobarAsync`,
`ExpedienteService.EnviarAsync`. `ICurrentUserService` con `GetUserId()`, `EsAdmin()`.

## Objetivo
Agregar notificaciones DENTRO del sistema (no email): una entidad `Notificacion` por usuario, creada
automáticamente en 3 momentos clave, con endpoints para listarlas y marcarlas como leídas. NO cambiar
el comportamiento de los endpoints existentes — solo AGREGAR la creación de la notificación como
efecto secundario, sin que pueda hacer fallar la operación principal si algo sale mal.

---

## 1. Nueva entidad `Notificacion` — `/Models/Notificacion.cs`

| Propiedad | Tipo | Notas |
|---|---|---|
| Id | int | PK |
| UsuarioId | int | destinatario (columna simple, SIN navegación — mismo patrón que otras entidades) |
| Mensaje | string | requerido, max 300 |
| ExpedienteId | int? | nullable, para poder linkear al expediente relacionado |
| Leida | bool | default false |
| FechaCreacion | DateTime | default UtcNow |

En `AppDbContext`: `DbSet<Notificacion>`, sin FK obligatoria a Usuario (columna simple, igual que
`Subsanacion.UsuarioId`). FK opcional a Expediente con `OnDelete(SetNull)` si el expediente se
borra (aunque en la práctica los expedientes enviados no se borran, usar SetNull por seguridad, no
Cascade, para no perder notificaciones antiguas si algo cambia).

Migración: `dotnet ef migrations add AgregarNotificaciones` + `database update`. Verificar que es
puramente aditiva (una tabla nueva).

---

## 2. Servicio — `/Services/INotificacionService.cs` + `NotificacionService.cs`

```
Task CrearAsync(int usuarioId, string mensaje, int? expedienteId, CancellationToken ct);
Task<List<NotificacionDto>> ListarMiasAsync(CancellationToken ct);  // usa ICurrentUserService.GetUserId()
Task<int> ContarNoLeidasAsync(CancellationToken ct);
Task MarcarLeidaAsync(int id, CancellationToken ct);  // solo si pertenece al usuario actual
```

`NotificacionDto`: Id, Mensaje, ExpedienteId, Leida, FechaCreacion.

`ListarMiasAsync`: notificaciones del usuario actual, ordenadas por FechaCreacion desc, las no
leídas primero (o simplemente orden por fecha desc, con `Leida` visible para que el front las
distinga — tu criterio, priorizá simplicidad).

`MarcarLeidaAsync`: si la notificación no existe o no pertenece al usuario actual → NotFoundException
(no reveles si existe de otro usuario). Si existe y es del usuario, `Leida = true`, SaveChanges.

Registrar `INotificacionService` en Program.cs.

---

## 3. Disparo en los 3 puntos existentes

**IMPORTANTE**: la creación de la notificación debe estar envuelta en try/catch (o ser lo último que
ocurre, después del SaveChanges principal) para que un fallo al notificar NUNCA tumbe la operación
principal (aprobar, solicitar subsanación, enviar). Si falla, loguear con ILogger y continuar,
devolviendo igual el resultado exitoso de la operación principal.

- En `RevisionService.SolicitarSubsanacionAsync`, después de guardar exitosamente: notificar al
  `expediente.UsuarioId` con mensaje tipo `"Tu expediente '{expediente.Entidad}' requiere subsanación."`
  y `expedienteId = expediente.Id`.
- En `RevisionService.AprobarAsync`, después de guardar: notificar al `expediente.UsuarioId` con
  `"Tu expediente '{expediente.Entidad}' fue aprobado (Nº {numeroExpediente})."`.
- En `ExpedienteService.EnviarAsync`, después de guardar: notificar a TODOS los usuarios con
  `Rol == "Admin"` y `Activo == true` (query simple a Usuarios) con
  `"El expediente '{expediente.Entidad}' fue enviado para revisión."` — crear una notificación por
  cada admin activo.

Inyectar `INotificacionService` en `RevisionService` y `ExpedienteService` (agregar al constructor,
registrar la dependencia si hace falta ajustar Program.cs).

---

## 4. Controller — `/Controllers/NotificacionesController.cs`

`[Authorize]` (cualquier usuario autenticado, no solo Admin — cada quien ve las suyas).
Ruta base `api/notificaciones`:
- **GET `""`** → 200 `List<NotificacionDto>` (las del usuario actual).
- **GET `"no-leidas/count"`** → 200 `{ "count": N }`.
- **PATCH `"{id:int}/leer"`** → 204.

---

## Al terminar
- `dotnet build` limpio. Migración aplicada y confirmada como aditiva.
- Probar real:
  1. Como Usuario: crear+completar+enviar un expediente → verificar que TODOS los admins activos
     (al menos el dev) recibieron una notificación (GET /notificaciones con el token del admin).
  2. Como Admin: solicitar subsanación sobre ese expediente → el Usuario dueño recibe notificación
     (GET /notificaciones con el token del usuario).
  3. Como Admin: aprobar un expediente → el dueño recibe notificación de aprobación.
  4. GET `/no-leidas/count` refleja el número correcto: PATCH `/leer` sobre una → el count baja en 1.
  5. Confirmar que aprobar/solicitar-subsanación/enviar siguen devolviendo 200 normalmente aunque
     la notificación fallara (podés simular esto comentando temporalmente algo si querés probarlo,
     o confía en el try/catch si el flujo normal ya funciona).
- Reportar resultados y limpiar datos de prueba (dejar usuario dev).
