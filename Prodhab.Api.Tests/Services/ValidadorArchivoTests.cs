using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Prodhab.Api.Configuracion;
using Prodhab.Api.Services;

namespace Prodhab.Api.Tests.Services;

// ValidadorArchivo es código puro (sin base de datos ni I/O real): construimos los
// IFormFile en memoria con MemoryStream, sin tocar el disco. Todos los tests usan el
// tamaño máximo por defecto (10 MB) salvo el de tamaño excedido, que arma su propia
// instancia de AlmacenamientoOptions con un límite chico para no tener que generar
// un archivo real de varios MB.
public class ValidadorArchivoTests
{
    private static readonly byte[] FirmaPdf = [0x25, 0x50, 0x44, 0x46, 0x2D]; // %PDF-

    private static ValidadorArchivo CrearValidador(long tamanoMaximoBytes = 10 * 1024 * 1024)
    {
        var opciones = Options.Create(new AlmacenamientoOptions { TamanoMaximoBytes = tamanoMaximoBytes });
        return new ValidadorArchivo(opciones);
    }

    private static IFormFile CrearFormFile(byte[] contenido, string nombreArchivo)
    {
        var stream = new MemoryStream(contenido);
        return new FormFile(stream, 0, contenido.Length, "archivo", nombreArchivo)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream", // deliberadamente "mentiroso": el validador no debe confiar en esto.
        };
    }

    // Arma un .docx real (ZIP con word/document.xml) en memoria.
    private static byte[] CrearDocxValido()
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entrada = zip.CreateEntry("word/document.xml");
            using var writer = new StreamWriter(entrada.Open());
            writer.Write("<?xml version=\"1.0\"?><document/>");
        }
        return buffer.ToArray();
    }

    // Arma un ZIP real pero sin la entrada que identifica a un .docx.
    private static byte[] CrearZipSinDocumentoWord()
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entrada = zip.CreateEntry("otra-cosa.txt");
            using var writer = new StreamWriter(entrada.Open());
            writer.Write("no soy un word");
        }
        return buffer.ToArray();
    }

    [Fact]
    public async Task ValidarAsync_PdfConFirmaCorrecta_EsValido()
    {
        var contenido = FirmaPdf.Concat(Encoding.ASCII.GetBytes("resto del pdf")).ToArray();
        var archivo = CrearFormFile(contenido, "protocolo.pdf");
        var validador = CrearValidador();

        var resultado = await validador.ValidarAsync(archivo, CancellationToken.None);

        Assert.True(resultado.EsValido);
        Assert.Equal("application/pdf", resultado.MimeDetectado);
        Assert.Equal("pdf", resultado.Extension);
        Assert.Null(resultado.Error);
    }

    [Fact]
    public async Task ValidarAsync_TextoRenombradoAPdf_SeRechaza()
    {
        var contenido = Encoding.ASCII.GetBytes("hola mundo, esto no es un pdf");
        var archivo = CrearFormFile(contenido, "documento.pdf");
        var validador = CrearValidador();

        var resultado = await validador.ValidarAsync(archivo, CancellationToken.None);

        Assert.False(resultado.EsValido);
        Assert.NotNull(resultado.Error);
    }

    [Fact]
    public async Task ValidarAsync_DocxConEntradaDocumentoWord_EsValido()
    {
        var contenido = CrearDocxValido();
        var archivo = CrearFormFile(contenido, "protocolo.docx");
        var validador = CrearValidador();

        var resultado = await validador.ValidarAsync(archivo, CancellationToken.None);

        Assert.True(resultado.EsValido);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            resultado.MimeDetectado);
        Assert.Equal("docx", resultado.Extension);
    }

    [Fact]
    public async Task ValidarAsync_ZipSinDocumentoWordRenombradoADocx_SeRechaza()
    {
        var contenido = CrearZipSinDocumentoWord();
        var archivo = CrearFormFile(contenido, "falso.docx");
        var validador = CrearValidador();

        var resultado = await validador.ValidarAsync(archivo, CancellationToken.None);

        Assert.False(resultado.EsValido);
        Assert.NotNull(resultado.Error);
    }

    [Fact]
    public async Task ValidarAsync_ExtensionNoPermitida_SeRechazaSinMirarContenido()
    {
        // Contenido válido de PDF, pero con extensión .exe: debe rechazar por extensión,
        // sin siquiera llegar a comparar magic bytes.
        var contenido = FirmaPdf.Concat(Encoding.ASCII.GetBytes("contenido")).ToArray();
        var archivo = CrearFormFile(contenido, "instalador.exe");
        var validador = CrearValidador();

        var resultado = await validador.ValidarAsync(archivo, CancellationToken.None);

        Assert.False(resultado.EsValido);
        Assert.Contains("PDF o Word", resultado.Error);
    }

    [Fact]
    public async Task ValidarAsync_ArchivoVacio_SeRechazaConMensajeDeVacio()
    {
        var archivo = CrearFormFile([], "vacio.pdf");
        var validador = CrearValidador();

        var resultado = await validador.ValidarAsync(archivo, CancellationToken.None);

        Assert.False(resultado.EsValido);
        Assert.Contains("vacío", resultado.Error);
    }

    [Fact]
    public async Task ValidarAsync_ExcedeTamanoMaximo_SeRechaza()
    {
        // AlmacenamientoOptions es una clase plana con setters: no hace falta mockear
        // nada, alcanza con instanciar un validador con un límite chico a propósito
        // para no tener que generar un archivo real de varios MB en el test.
        var contenido = FirmaPdf.Concat(new byte[50]).ToArray(); // 55 bytes
        var archivo = CrearFormFile(contenido, "protocolo.pdf");
        var validador = CrearValidador(tamanoMaximoBytes: 10); // límite de 10 bytes

        var resultado = await validador.ValidarAsync(archivo, CancellationToken.None);

        Assert.False(resultado.EsValido);
        Assert.Contains("tamaño máximo", resultado.Error);
    }
}
