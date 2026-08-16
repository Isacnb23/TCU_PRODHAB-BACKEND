using System.IO.Compression;
using Microsoft.Extensions.Options;
using Prodhab.Api.Configuracion;

namespace Prodhab.Api.Services;

// Valida por CONTENIDO real (magic bytes + estructura), nunca por archivo.ContentType,
// que lo manda el navegador y es trivial de falsificar.
public class ValidadorArchivo : IValidadorArchivo
{
    private const string ExtensionPdf = "pdf";
    private const string ExtensionDocx = "docx";

    private const string MimePdf = "application/pdf";
    private const string MimeDocx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    // Todo .docx es un OPC (zip) que obligatoriamente contiene esta parte.
    private const string EntradaDocumentoWord = "word/document.xml";

    private const string ErrorPdfInvalido = "El archivo no es un PDF válido.";
    private const string ErrorDocxInvalido = "El archivo no es un documento Word (.docx) válido.";

    private static readonly byte[] FirmaPdf = [0x25, 0x50, 0x44, 0x46, 0x2D]; // %PDF-
    private static readonly byte[] FirmaZip = [0x50, 0x4B, 0x03, 0x04];       // PK\x03\x04

    private readonly AlmacenamientoOptions _opciones;

    public ValidadorArchivo(IOptions<AlmacenamientoOptions> opciones)
    {
        _opciones = opciones.Value;
    }

    public async Task<ResultadoValidacionArchivo> ValidarAsync(IFormFile archivo, CancellationToken ct)
    {
        // 1. Archivo presente y con contenido.
        if (archivo is null || archivo.Length == 0)
        {
            return ResultadoValidacionArchivo.Invalido("El archivo está vacío.");
        }

        // 2. Tamaño: se corta acá para que bufferizar en memoria sea seguro.
        if (archivo.Length > _opciones.TamanoMaximoBytes)
        {
            var maximoMb = _opciones.TamanoMaximoBytes / (1024d * 1024d);
            return ResultadoValidacionArchivo.Invalido(
                $"El archivo supera el tamaño máximo permitido ({maximoMb:0.##} MB).");
        }

        // 3. Extensión declarada dentro del allowlist.
        var extension = Path.GetExtension(archivo.FileName).TrimStart('.').ToLowerInvariant();

        if (extension is not (ExtensionPdf or ExtensionDocx))
        {
            return ResultadoValidacionArchivo.Invalido("Solo se permiten archivos PDF o Word (.docx).");
        }

        // Bufferizar para poder mirar los magic bytes y abrir el zip sin consumir el stream
        // que después usa el almacenamiento.
        using var buffer = new MemoryStream();
        await using (var origen = archivo.OpenReadStream())
        {
            await origen.CopyToAsync(buffer, ct);
        }

        // 4 y 5. El contenido tiene que coincidir con lo que declara la extensión:
        // un archivo renombrado no pasa.
        return extension == ExtensionPdf
            ? ValidarPdf(buffer)
            : ValidarDocx(buffer);
    }

    private static ResultadoValidacionArchivo ValidarPdf(MemoryStream buffer)
    {
        return EmpiezaCon(buffer, FirmaPdf)
            ? ResultadoValidacionArchivo.Valido(MimePdf, ExtensionPdf)
            : ResultadoValidacionArchivo.Invalido(ErrorPdfInvalido);
    }

    private static ResultadoValidacionArchivo ValidarDocx(MemoryStream buffer)
    {
        if (!EmpiezaCon(buffer, FirmaZip))
        {
            return ResultadoValidacionArchivo.Invalido(ErrorDocxInvalido);
        }

        // Un zip cualquiera pasa la firma: hay que confirmar que además es un documento Word.
        try
        {
            buffer.Position = 0;
            using var zip = new ZipArchive(buffer, ZipArchiveMode.Read, leaveOpen: true);

            if (zip.GetEntry(EntradaDocumentoWord) is null)
            {
                return ResultadoValidacionArchivo.Invalido(ErrorDocxInvalido);
            }
        }
        catch (InvalidDataException)
        {
            // Firma zip válida pero estructura rota.
            return ResultadoValidacionArchivo.Invalido(ErrorDocxInvalido);
        }

        return ResultadoValidacionArchivo.Valido(MimeDocx, ExtensionDocx);
    }

    private static bool EmpiezaCon(MemoryStream buffer, byte[] firma)
    {
        if (buffer.Length < firma.Length)
        {
            return false;
        }

        buffer.Position = 0;

        Span<byte> cabecera = stackalloc byte[firma.Length];
        buffer.ReadExactly(cabecera);

        return cabecera.SequenceEqual(firma);
    }
}
