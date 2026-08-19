using Microsoft.EntityFrameworkCore;
using Prodhab.Api.Configuracion;
using Prodhab.Api.Models;

namespace Prodhab.Api.Data;

public static class DbSeeder
{
    // Siembra el primer administrador a partir de la sección "AdminInicial" de configuración.
    // Corre en cualquier entorno; si ya existe algún Usuario, no hace nada (idempotente).
    public static async Task SeedAdminInicialAsync(AppDbContext db, AdminInicialOptions adminInicial, ILogger logger)
    {
        if (await db.Usuarios.AnyAsync())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(adminInicial.Email) || string.IsNullOrWhiteSpace(adminInicial.Password))
        {
            logger.LogWarning(
                "No hay AdminInicial configurado; cree el primer usuario administrador manualmente.");
            return;
        }

        db.Usuarios.Add(new Usuario
        {
            Nombre = string.IsNullOrWhiteSpace(adminInicial.Nombre) ? "Administrador" : adminInicial.Nombre,
            Email = adminInicial.Email,
            Rol = "Admin",
            Activo = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminInicial.Password)
        });

        await db.SaveChangesAsync();
    }
}
