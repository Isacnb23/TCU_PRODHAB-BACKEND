# Sistema Web de Protocolos de Actuación — Backend (PRODHAB)

API REST para la gestión de expedientes de protocolos de actuación (Ley 8968) de la Agencia de
Protección de Datos de los Habitantes (PRODHAB).

## Stack

- ASP.NET Core Web API (.NET 10)
- Entity Framework Core + SQL Server
- Autenticación JWT con roles (Admin / Usuario)
- Almacenamiento de archivos en disco con validación por contenido

## Arquitectura

Capas simples y mantenibles: **Controller → Servicio → DbContext**. Sin patrón repositorio ni MediatR
(el `DbContext` de EF Core ya cumple ese rol). Manejo de errores centralizado con `ProblemDetails`.

Entidades principales: `Expediente`, `DatosFormulario` (datos por paso del formulario),
`Subsanacion`, `Observacion`, `Usuario`, `HistorialExpediente`.

## Ciclo de vida del expediente

```
Borrador → Enviado → (RequiereSubsanacion ⇄ Enviado) → Aprobado
```

- El **Usuario** crea un borrador, completa el formulario por pasos y lo envía.
- El **Admin** (PRODHAB) revisa: aprueba (asignando el número de expediente) o solicita subsanación
  con observaciones por paso.
- El **Usuario** corrige, adjunta subsanaciones y reenvía.

## Ejecución en desarrollo

Requisitos: .NET 10 SDK y SQL Server LocalDB (incluido con Visual Studio).

```bash
# Restaurar dependencias
dotnet restore

# La configuración de desarrollo (appsettings.Development.json) ya trae la cadena de
# conexión a LocalDB, la clave JWT de desarrollo y un usuario administrador de prueba.

# Crear el esquema de la base (aplica las migraciones)
dotnet ef database update

# Ejecutar
dotnet run
```

En desarrollo se crea automáticamente el usuario **dev@prodhab.local / dev123** (rol Admin).
La API expone la especificación OpenAPI en `/openapi/v1.json` y un endpoint de salud en `/health`.

## Configuración por entorno

Los secretos y valores específicos del entorno **no están en el repositorio**. En producción se
configuran por variables de entorno:

- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__DefaultConnection`
- `Jwt__Key` (mínimo 32 caracteres)
- `Cors__AllowedOrigins__0` (dominio del frontend)
- `AdminInicial__Nombre`, `AdminInicial__Email`, `AdminInicial__Password` (primer administrador,
  creado en el primer arranque si la base no tiene usuarios)

El archivo `appsettings.Production.json` es una plantilla de referencia sin secretos.

## Despliegue

Ver **`MANUAL_INSTALACION.md`** para la guía completa de instalación en producción (Windows/IIS y
Linux), incluyendo la base de datos SQL Server Express y el frontend.

El esquema de base de datos para producción se crea con el script idempotente
`Migrations/prodhab-esquema-completo.sql`.
