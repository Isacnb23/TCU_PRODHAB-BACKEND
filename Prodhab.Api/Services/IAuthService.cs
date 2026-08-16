using Prodhab.Api.DTOs.Auth;

namespace Prodhab.Api.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginDto dto, CancellationToken ct);
}
