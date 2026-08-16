using Prodhab.Api.Exceptions;
using Prodhab.Api.Models;

namespace Prodhab.Api.Services;

// Regla de propiedad compartida por expedientes, pasos y subsanaciones: pasa el dueño o un Admin.
// Vive acá para no duplicarla en los tres servicios ni acoplarlos entre sí.
internal static class AccesoExpediente
{
    public static void Verificar(Expediente expediente, ICurrentUserService currentUser)
    {
        if (currentUser.EsAdmin() || expediente.UsuarioId == currentUser.GetUserId())
        {
            return;
        }

        throw new ForbiddenException("No tiene acceso a este expediente.");
    }
}
