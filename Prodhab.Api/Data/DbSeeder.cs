using Microsoft.EntityFrameworkCore;
using Prodhab.Api.Models;

namespace Prodhab.Api.Data;

public static class DbSeeder
{
    // Siembra el usuario de desarrollo. Solo se invoca en el entorno Development.
    public static async Task SeedDevAsync(AppDbContext db)
    {
        if (await db.Usuarios.AnyAsync())
        {
            return;
        }

        db.Usuarios.Add(new Usuario
        {
            Nombre = "Usuario de Desarrollo",
            Email = "dev@prodhab.local",
            Rol = "Admin",
            Activo = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("dev123")
        });

        await db.SaveChangesAsync();
    }
}
