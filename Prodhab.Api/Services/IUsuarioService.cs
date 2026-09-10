using Prodhab.Api.DTOs.Usuarios;

namespace Prodhab.Api.Services;

public interface IUsuarioService
{
    Task<UsuarioDto> CrearAsync(CrearUsuarioDto dto, CancellationToken ct);

    Task<List<UsuarioDto>> ListarAsync(CancellationToken ct);

    Task DesactivarAsync(int id, CancellationToken ct);

    Task<UsuarioDto> ObtenerPorIdAsync(int id, CancellationToken ct);

    // Genera y guarda una contraseña temporal nueva, devolviéndola en texto plano
    // (única vez que existe así: no se persiste en ningún lado sin hashear).
    Task<string> ResetearPasswordAsync(int id, CancellationToken ct);
}
