# Sistema de Expedientes PRODHAB - Backend

API backend del Sistema de Expedientes de PRODHAB (Costa Rica), que da soporte al wizard de protocolos de actuación, gestión de expedientes, subsanaciones y autenticación de usuarios.

## Stack

- **.NET 10** (ASP.NET Core Web API)
- **Entity Framework Core 10** + SQL Server
- **JWT Bearer** para autenticación
- **BCrypt.Net** para hash de contraseñas

## Cómo levantar el proyecto localmente

### Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- SQL Server LocalDB (incluido con Visual Studio) o una instancia de SQL Server accesible

### Pasos

```bash
# 1. Restaurar dependencias
dotnet restore

# 2. Aplicar migraciones y crear/actualizar la base de datos
dotnet ef database update --project Prodhab.Api --startup-project Prodhab.Api

# 3. Ejecutar la API
dotnet run --project Prodhab.Api
```

La API quedará disponible según los puertos configurados en `Prodhab.Api/Properties/launchSettings.json`.

## Configuración y variables de entorno

`appsettings.json` incluye una clave JWT y una cadena de conexión **solo para desarrollo local** (LocalDB). Para un entorno de producción, estos valores **no deben vivir en el repositorio** y deben sobreescribirse mediante variables de entorno:

| Variable de entorno | Reemplaza a |
|---|---|
| `Jwt__Key` | `Jwt:Key` en `appsettings.json` |
| `ConnectionStrings__DefaultConnection` | `ConnectionStrings:DefaultConnection` en `appsettings.json` |

La carpeta `Storage/` (archivos subidos por usuarios, ej. subsanaciones) no se versiona en git.
