# Paso 5 — Autenticación JWT, roles y propiedad de expedientes (Prodhab.Api)

Backend .NET 10 / EF Core / SQL Server (LocalDB). Ya existe y funciona:
- Capa de datos intacta. La entidad `Usuario` ya tiene: Nombre, Email (único), PasswordHash (BCrypt),
  Rol (string, default "Usuario"), Activo, FechaCreacion.
- CRUD de Expedientes, guardado de pasos, subsanaciones con upload validado.
- `ICurrentUserService`/`CurrentUserService` (hoy devuelve el usuario dev de la BD — lo vamos a
  reemplazar por lectura del claim JWT).
- Manejo de errores central: `GlobalExceptionHandler` con `NotFoundException`→404 y
  `BusinessRuleException`→409. `DbSeeder` crea un usuario dev (dev@prodhab.local, Rol Admin, pass "dev123").
- Arquitectura Controller → Servicio → DbContext. BCrypt.Net-Next ya instalado.

## Alcance
Implementar autenticación JWT con roles y filtro de propiedad, saldando dos deudas técnicas
(CurrentUserService real + split 400/409). NO cambiar el esquema de la BD (la entidad Usuario ya
sirve). NO tocar la lógica de negocio de expedientes/pasos/subsanaciones salvo para AGREGAR los
chequeos de propiedad indicados.

---

## 1. Paquete NuGet
- `Microsoft.AspNetCore.Authentication.JwtBearer` (versión 10.x, compatible con .NET 10)

---

## 2. Configuración JWT

En `appsettings.json`, sección:
```json
"Jwt": {
  "Issuer": "Prodhab.Api",
  "Audience": "Prodhab.Client",
  "Key": "CLAVE-DE-DESARROLLO-SOLO-LOCAL-cambiar-en-produccion-min-32-caracteres-1234567890",
  "ExpiryHours": 8
}
```
La Key debe tener al menos 32 caracteres (HMAC-SHA256). Dejar un comentario/nota:
en PRODUCCIÓN la Key va por variable de entorno `Jwt__Key`, NO en el repo (se documenta en el manual).

Crear `/Configuracion/JwtOptions.cs` (namespace `Prodhab.Api.Configuracion`):
- `string Issuer`, `string Audience`, `string Key`, `int ExpiryHours`

Enlazar: `builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));`

---

## 3. DTOs de auth — `/DTOs/Auth` (namespace `Prodhab.Api.DTOs.Auth`)

**LoginDto.cs**
- `string Email` — [Required], [EmailAddress]
- `string Password` — [Required]

**LoginResponseDto.cs**
- `string Token`
- `DateTime ExpiraEn`
- `int UsuarioId`
- `string Nombre`
- `string Email`
- `string Rol`

---

## 4. Servicio de autenticación — `/Services`

**IAuthService.cs**
```
Task<LoginResponseDto> LoginAsync(LoginDto dto, CancellationToken ct);
```

**AuthService.cs** — recibe `AppDbContext` e `IOptions<JwtOptions>`.
- **LoginAsync**:
  1. Buscar Usuario por Email (case-insensitive) y `Activo == true`.
  2. Si no existe o `BCrypt.Verify(dto.Password, usuario.PasswordHash)` es false →
     lanzar `UnauthorizedException("Credenciales inválidas.")`.
     (Mismo mensaje para "no existe" y "password incorrecto" — no revelar cuál falló.)
  3. Generar el JWT (ver abajo) y devolver LoginResponseDto.

**Generación del token** (dentro de AuthService o en un helper `/Services/JwtTokenGenerator.cs`):
- Claims: `sub` = UsuarioId, `ClaimTypes.Role` = usuario.Rol, `ClaimTypes.NameIdentifier` = UsuarioId,
  `email` = Email, `name` = Nombre.
- Firmar con `SymmetricSecurityKey` (UTF8 de Jwt.Key) y `HmacSha256`.
- Issuer, Audience y expiración (`UtcNow.AddHours(ExpiryHours)`) desde JwtOptions.

---

## 5. Nueva excepción y split 400/409

En `/Exceptions`, agregar:
- **UnauthorizedException.cs** : Exception → 401
- **ForbiddenException.cs** : Exception → 403
- **ValidationException.cs** : Exception → 400

Actualizar `GlobalExceptionHandler` para mapear:
- `ValidationException` → 400 (title "Solicitud inválida")
- `UnauthorizedException` → 401 (title "No autenticado")
- `ForbiddenException` → 403 (title "Acceso denegado")
- `NotFoundException` → 404 (ya existe)
- `BusinessRuleException` → 409 (ya existe)
- otras → 500 (ya existe)

**Reclasificar errores existentes** (input inválido = 400, conflicto de estado = 409):
- En `DatosFormularioService`: "paso fuera de rango [1,9]" → `ValidationException` (era BusinessRule).
  Mantener "expediente no está en Borrador" como `BusinessRuleException` (409).
- En `SubsanacionService` y `ValidadorArchivo`: los errores de archivo (vacío, tamaño, extensión no
  permitida, no es PDF válido, no es docx válido) y la regla "justificación o archivo" y "paso fuera
  de rango" → `ValidationException` (400). Mantener "expediente Aprobado" como `BusinessRuleException` (409).

---

## 6. CurrentUserService real (leer del token)

Agregar `AddHttpContextAccessor()` en Program.cs.

Ampliar **ICurrentUserService**:
```
int GetUserId();
string GetRol();
bool EsAdmin();
```

Reescribir **CurrentUserService** para recibir `IHttpContextAccessor`:
- `GetUserId()`: leer el claim `sub` (o `ClaimTypes.NameIdentifier`) del usuario del HttpContext y
  parsear a int. Si no hay usuario autenticado → `UnauthorizedException("No hay un usuario autenticado.")`.
- `GetRol()`: leer el claim de rol.
- `EsAdmin()`: `GetRol() == "Admin"`.
Quitar la dependencia del AppDbContext en este servicio (ya no consulta la BD).
Eliminar el `// TODO` del puente temporal.

---

## 7. Gestión de usuarios (solo Admin) — `/Services` + `/Controllers`

DTOs en `/DTOs/Usuarios`:
- **CrearUsuarioDto.cs**: Nombre [Required, MaxLength 200], Email [Required, EmailAddress],
  Password [Required, MinLength 6], Rol [Required] (validar que sea "Admin" o "Usuario").
- **UsuarioDto.cs** (response, SIN PasswordHash): Id, Nombre, Email, Rol, Activo, FechaCreacion.

**IUsuarioService.cs** / **UsuarioService.cs** (recibe AppDbContext):
- `CrearAsync(CrearUsuarioDto dto)`: validar que el Rol sea "Admin" o "Usuario" (si no →
  ValidationException). Verificar que el Email no exista (si existe → BusinessRuleException
  "Ya existe un usuario con ese correo."). Hashear el password con BCrypt. Crear y guardar. Devolver UsuarioDto.
- `ListarAsync()`: todos los usuarios como UsuarioDto (proyección inline).
- `DesactivarAsync(int id)`: NotFound si no existe; setear Activo=false. (No borrar usuarios — desactivar.)

**UsuariosController** `[Authorize(Roles = "Admin")]`, `[Route("api/usuarios")]`:
- POST `""` → 201 UsuarioDto
- GET `""` → 200 List<UsuarioDto>
- PATCH `"{id:int}/desactivar"` → 204

Registrar `IAuthService` y `IUsuarioService` en Program.cs (AddScoped).

---

## 8. AuthController — `/Controllers/AuthController.cs`

`[Route("api/auth")]`. NO poner [Authorize] a nivel de controller.
- POST `"login"` — `[AllowAnonymous]`, body LoginDto → 200 LoginResponseDto | 401.
- GET `"me"` — `[Authorize]`, devuelve los datos del usuario actual (desde ICurrentUserService +
  consulta a la BD por su Id) como UsuarioDto → 200 | 401.

---

## 9. Wiring de autenticación/autorización en Program.cs

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // patrón del proyecto
        var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = "sub"
        };
    });
builder.Services.AddAuthorization();
```
Y en el pipeline, en este orden: `app.UseAuthentication();` ANTES de `app.UseAuthorization();`
(ambos después de UseCors y del exception handler).

---

## 10. Proteger endpoints + filtro de propiedad

Poner `[Authorize]` a nivel de controller en: `ExpedientesController`, `PasosController`,
`SubsanacionesController`. (`UsuariosController` ya lleva `[Authorize(Roles="Admin")]`; `AuthController` abierto en login.)

**Filtro de propiedad** — la lógica va en la CAPA DE SERVICIO (no en el controller):

En `ExpedienteService`, agregar un helper privado:
```
private async Task<Expediente> CargarConAccesoAsync(int id, CancellationToken ct)
```
que carga el expediente (NotFound si no existe) y verifica acceso: si `!_currentUser.EsAdmin()` y
`expediente.UsuarioId != _currentUser.GetUserId()` → `ForbiddenException("No tiene acceso a este expediente.")`.
Usar este helper en `ObtenerPorIdAsync`, `ActualizarAsync`, `EliminarAsync`.

- `ListarPorUsuarioAsync` → renombrar conceptualmente: si `EsAdmin()` devuelve TODOS los expedientes;
  si no, solo los del `GetUserId()`. (El controller GET `/api/expedientes` ya no recibe usuarioId por
  query; usa el usuario actual.)
- `CrearAsync`: el UsuarioId del nuevo expediente = `_currentUser.GetUserId()`.

En `DatosFormularioService` y `SubsanacionService`: antes de operar sobre un expediente, aplicar el
MISMO chequeo de propiedad (Admin o dueño; si no → ForbiddenException). Para no duplicar, pueden
recibir `ICurrentUserService` y replicar el helper, o exponer un método de verificación reutilizable.
Elegí la opción más limpia sin crear acoplamientos raros.

---

## Al terminar
- `dotnet build` limpio.
- Levantar la API y probar el flujo REAL:
  1. POST `/api/auth/login` con dev@prodhab.local / dev123 → 200 con token y rol Admin.
  2. GET `/api/expedientes` SIN token → 401.
  3. GET `/api/expedientes` con el token → 200.
  4. Como Admin, POST `/api/usuarios` creando un usuario normal (rol "Usuario") → 201.
  5. Login con ese usuario nuevo → token con rol Usuario.
  6. Con el token del Usuario: POST crear expediente → 201 (queda como dueño).
  7. Con el token del Usuario: GET `/api/expedientes` → solo ve el suyo.
  8. Como Admin: GET `/api/expedientes` → ve TODOS (el del usuario y cualquier otro).
  9. Crear un expediente como Admin, anotar su id. Con el token del Usuario, GET ese id → 403.
  10. Usuario intenta PUT/DELETE/guardar paso/subsanar sobre un expediente ajeno → 403.
  11. POST `/api/usuarios` con un usuario NO admin (usar token de Usuario) → 403.
  12. Split 400/409: subir un .txt renombrado a .pdf → ahora **400** (antes 409). Expediente Aprobado +
      subsanar → sigue **409**. Confirmar que los dos casos ya se distinguen.
  13. GET `/api/auth/me` con token → datos del usuario actual.
- Reportar resultados, árbol de archivos nuevos/modificados, y confirmar que los servicios y
  controllers de expedientes/pasos/subsanaciones NO cambiaron su firma pública (solo se agregaron los
  chequeos de propiedad y el [Authorize]).
- Limpiar datos de prueba (borrar expedientes y usuarios de prueba; dejar el usuario dev).
