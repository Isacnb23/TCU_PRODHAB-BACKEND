# Esquema de datos — Prodhab.Api

Backend de sistema de expedientes para PRODHAB (Costa Rica).
Proyecto: **Prodhab.Api** — ASP.NET Core Web API, **.NET 10**, EF Core + SQL Server (LocalDB).
El proyecto ya está scaffoldeado (plantilla Web API con controllers) y `appsettings.json` ya
trae `ConnectionStrings:DefaultConnection` apuntando a `(localdb)\MSSQLLocalDB / ProdhabDb`.

## Alcance de esta tarea
Crear **solo la capa de datos**: modelos + DbContext + migración inicial.
**NO** crear controllers ni lógica de negocio todavía.

Antes de modificar, leé `Program.cs` y `appsettings.json` actuales.

---

## 1. Paquetes NuGet (versiones 10.x, compatibles con .NET 10)
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Design`
- `Microsoft.EntityFrameworkCore.Tools`

Asegurar que la herramienta CLI esté en v10:
```
dotnet tool update --global dotnet-ef
```

---

## 2. Carpeta `/Models` — namespace `Prodhab.Api.Models`

### EstadoExpediente.cs (enum)
```
Borrador, Enviado, EnRevision, RequiereSubsanacion, Aprobado
```

### Expediente.cs
| Propiedad | Tipo | Notas |
|---|---|---|
| Id | int | PK |
| NumeroExpediente | string? | **NULLABLE**. PRODHAB lo asigna tras el 1er envío. Formato ej: `001-01-2026-INS`. Max 50 |
| Entidad | string | requerido, max 300 |
| Anio | int | |
| Estado | EstadoExpediente | default = Borrador |
| PasoActual | int | default = 1 (para retomar el wizard) |
| UsuarioId | int | |
| FechaCreacion | DateTime | default = DateTime.UtcNow |
| FechaModificacion | DateTime | default = DateTime.UtcNow |
| FechaEnvio | DateTime? | nullable |

Navegación:
- `Usuario? Usuario`
- `ICollection<DatosFormulario> Datos`
- `ICollection<Subsanacion> Subsanaciones`
- `ICollection<HistorialExpediente> Historial`

### DatosFormulario.cs
| Propiedad | Tipo | Notas |
|---|---|---|
| Id | int | PK |
| ExpedienteId | int | FK |
| Paso | int | 1..9 |
| DatosJson | string | default = "{}", nvarchar(max), JSON completo del paso |
| Completado | bool | |
| FechaActualizacion | DateTime | default = DateTime.UtcNow |

Navegación: `Expediente? Expediente`

### Subsanacion.cs
| Propiedad | Tipo | Notas |
|---|---|---|
| Id | int | PK |
| ExpedienteId | int | FK |
| Paso | int | alineado con DatosFormulario.Paso (NO texto libre) |
| Campo | string | max 200 |
| TextoJustificacion | string? | nullable |
| ArchivoRuta | string? | GUID físico en disco |
| ArchivoNombre | string? | nombre ORIGINAL del usuario (nunca usar como nombre físico), max 300 |
| ArchivoMimeType | string? | MIME real validado por buffer (no el del navegador), max 150 |
| ArchivoExtension | string? | max 10 |
| ArchivoTamanoBytes | long? | |
| ArchivoHash | string? | SHA256, max 64 |
| UsuarioId | int | |
| FechaCreacion | DateTime | default = DateTime.UtcNow |
| FechaSubsanacion | DateTime | default = DateTime.UtcNow |

Navegación: `Expediente? Expediente`

### Usuario.cs
| Propiedad | Tipo | Notas |
|---|---|---|
| Id | int | PK |
| Nombre | string | requerido, max 200 |
| Email | string | requerido, max 200, único |
| PasswordHash | string | BCrypt |
| Rol | string | default = "Usuario" (Usuario / Admin), max 50 |
| Activo | bool | default = true |
| FechaCreacion | DateTime | default = DateTime.UtcNow |

### HistorialExpediente.cs
| Propiedad | Tipo | Notas |
|---|---|---|
| Id | int | PK |
| ExpedienteId | int | FK |
| UsuarioId | int | |
| Accion | string | ej: "CambioEstado", "GuardadoPaso". requerido, max 100 |
| Detalle | string? | nullable |
| FechaCambio | DateTime | default = DateTime.UtcNow |

Navegación: `Expediente? Expediente`

---

## 3. Carpeta `/Data` — `AppDbContext.cs` (namespace `Prodhab.Api.Data`)

Configuraciones en `OnModelCreating`:

**Expediente**
- `Estado` con `HasConversion<string>()` y `HasMaxLength(30)` (enum guardado como texto, no int)
- Índice **ÚNICO FILTRADO** en `NumeroExpediente`:
  `HasFilter("[NumeroExpediente] IS NOT NULL")`
  (permite múltiples borradores con número null, pero único cuando ya está asignado)
- FK a `Usuario` con `OnDelete(DeleteBehavior.Restrict)`

**DatosFormulario**
- `DatosJson` como `nvarchar(max)`
- Índice **ÚNICO compuesto** en `{ ExpedienteId, Paso }`
- FK a `Expediente` con `OnDelete(Cascade)`

**Subsanacion**
- CHECK constraint llamado `CK_Subsanacion_JustificacionOArchivo`:
  ```
  ([TextoJustificacion] IS NOT NULL AND LEN([TextoJustificacion]) > 0) OR [ArchivoRuta] IS NOT NULL
  ```
  Usar la sintaxis nueva de EF Core 10:
  `entity.ToTable(t => t.HasCheckConstraint("CK_Subsanacion_JustificacionOArchivo", "..."))`
- FK a `Expediente` con `OnDelete(Cascade)`

**Usuario**
- `Email` único

**HistorialExpediente**
- FK a `Expediente` con `OnDelete(Cascade)`

### ⚠ IMPORTANTE — evitar "multiple cascade paths"
NO configurar FK de navegación hacia `Usuario` en `Subsanacion`, `DatosFormulario` ni
`HistorialExpediente`. Dejar `UsuarioId` como columna simple (int) **sin** navegación.
Solo `Expediente` tiene navegación a `Usuario`. Esto evita el error de SQL Server de
múltiples rutas de cascada.

---

## 4. Registrar el DbContext en `Program.cs`
Leyendo la cadena `DefaultConnection`:
```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

---

## 5. Borrar archivos de ejemplo de la plantilla
- `WeatherForecast.cs`
- `Controllers/WeatherForecastController.cs`

---

## 6. Generar y aplicar la migración inicial
```
dotnet ef migrations add InitialCreate
dotnet ef database update
```
La BD `ProdhabDb` ya existe en LocalDB (creada a mano). EF Core solo le agrega las tablas.

---

## Al terminar
- Mostrar el árbol de archivos creado (`/Models` y `/Data`).
- Confirmar que la migración `InitialCreate` se aplicó **sin errores** (prestar atención
  especial al CHECK constraint y al índice filtrado, que suelen fallar si quedan mal escritos).
