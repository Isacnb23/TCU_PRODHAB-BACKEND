namespace Prodhab.Api.DTOs.Usuarios;

// La contraseña temporal viaja en texto plano SOLO en esta respuesta: no se guarda
// en ningún lado así, únicamente su hash. Es responsabilidad del Admin copiarla y
// comunicarla al usuario por fuera del sistema (no hay envío de correo).
public class ResetearPasswordDto
{
    public string PasswordTemporal { get; set; } = string.Empty;
}
