namespace Prodhab.Api.Services;

public interface ICurrentUserService
{
    int GetUserId();

    string GetRol();

    bool EsAdmin();
}
