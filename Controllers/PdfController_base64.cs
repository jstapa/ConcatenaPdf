using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Concatena.Pdf.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class PdfController : ControllerBase
    {
        private const int MaxFiles = 20; // Seguridad: máximo 20 archivos
        private const int MaxPdfSizeBytes = 5_000; // Máximo 5MB por PDF
        
        [HttpPost("concatenar")]
        public IActionResult ConcatenarPdfs([FromBody] PdfRequestDto request)
        {

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var pdfsBase64 = request.PdfsBase64;

            if (pdfsBase64 == null || !pdfsBase64.Any())
                return BadRequest("No se recibieron PDFs.");

            if (pdfsBase64.Count > MaxFiles)
                return BadRequest($"Máximo {MaxFiles} archivos permitidos.");

            var memStream = new MemoryStream();

            try
            {
                using var writer = new PdfWriter(memStream);
                using var outputPdf = new PdfDocument(writer);
                var merger = new PdfMerger(outputPdf);

                foreach (var base64 in pdfsBase64)
                {
                    byte[] pdfBytes;

                    try
                    {
                        pdfBytes = Convert.FromBase64String(base64);
                    }
                    catch
                    {
                        return BadRequest("Uno de los archivos no es Base64 válido.");
                    }

                    if (pdfBytes.Length > MaxPdfSizeBytes)
                        return BadRequest("Un archivo excede el tamaño permitido.");

                    try
                    {
                        using var readerStream = new MemoryStream(pdfBytes);
                        using var reader = new PdfReader(readerStream);
                        using var sourcePdf = new PdfDocument(reader);

                        if (sourcePdf.GetNumberOfPages() == 0)
                            return BadRequest("Uno de los archivos PDF está vacío.");

                        merger.Merge(sourcePdf, 1, sourcePdf.GetNumberOfPages());
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, $"Error al procesar un PDF: {ex.Message}");
                    }
                }

                outputPdf.Close();

                var resultBytes = memStream.ToArray();
                var resultBase64 = Convert.ToBase64String(resultBytes);

                return Ok(new
                {
                    PdfBase64 = resultBase64,
                    Message = "PDFs concatenados correctamente"
                });
            }
            catch (Exception ex)
            {
                // Loguear (usá tu propio sistema de logs)
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
    }
    public class PdfRequestDto
    {
        public List<string> PdfsBase64 { get; set; }
    }
}
