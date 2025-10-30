using Concatena.Pdf2;
using iText.Forms;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text;
using log4net;

namespace Concatena.Pdf.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class PdfController : ControllerBase
    {
        
        //private const int MaxFiles = 101; // Seguridad: máximo 20 archivos
        //private const int MaxPdfSizeBytes = 5_000_000; // Máximo 5MB por PDF
        private readonly ConcatenadorPdfSettings _settings;
        private readonly ILog _log;
        public PdfController(IOptions<ConcatenadorPdfSettings> settings, ILog log)
        {
            _settings = settings.Value;
            _log = log;
        }
        [HttpPost("insertar-texto-Pdf")]
        public async Task<IActionResult> InsertarTextoPdf(
            IFormFile archivo,
            [FromForm] ParametrosVisualizacion campos)
        {
            Inserciones inserciones = new Inserciones();
            PdfOptimizer pdf = new PdfOptimizer();
            if (archivo == null || archivo.Length == 0)
                return BadRequest("Archivo no recibido");

            using var ms = new MemoryStream();
            await archivo.CopyToAsync(ms);


            // Procesar el PDF (por ejemplo: optimizar, insertar campos, etc.)
            byte[] resultado = await inserciones.InsertarTextoImagenPdfXY(ms.ToArray(), campos.txt, campos.angulo, campos.opacity, campos.xt, campos.yt, campos.tFuente);
            //byte[] resultado = await pdf.OptimizarPdfAsync(camposBytes, 85);

            return File(resultado, "application/pdf", "archivo_procesado.pdf");
        }
        [HttpPost("insertar-optimizar")]
        public async Task<IActionResult> InsertarOptimizarPdf(
            IFormFile archivo,
            [FromForm] ParametrosVisualizacion campos)
        {
            Inserciones inserciones = new Inserciones();
            PdfOptimizer pdf = new PdfOptimizer();
            if (archivo == null || archivo.Length == 0)
                return BadRequest("Archivo no recibido");

            using var ms = new MemoryStream();
            await archivo.CopyToAsync(ms);
            

            // Procesar el PDF (por ejemplo: optimizar, insertar campos, etc.)
            byte[] camposBytes = await inserciones.InsertarTextoImagenPdfXY( ms.ToArray(), campos.txt,campos.angulo,campos.opacity, campos.xt, campos.yt,campos.tFuente);
            byte[] resultado =  pdf.OptimizarPdfAsync(camposBytes, 85);

            return File(resultado, "application/pdf", "archivo_procesado.pdf");
        }

        [HttpPost("concatenar-optimizar-Pdf")]
        public IActionResult ConcatenarOptimizarPdf(List<IFormFile> files)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (files == null || files.Count == 0)
                return BadRequest("No se recibieron archivos PDF.");

            if (files.Count > _settings.MaxFiles)
                return BadRequest($"Máximo {_settings.MaxFiles} archivos permitidos.");

            using var memStream = new MemoryStream();

            try
            {
                // Concatenar PDFs
                using (var writer = new PdfWriter(memStream, new WriterProperties().SetFullCompressionMode(true)))
                using (var outputPdf = new PdfDocument(writer))
                {
                    var merger = new PdfMerger(outputPdf);

                    foreach (var formFile in files)
                    {
                        if (formFile.Length == 0)
                            return BadRequest("Uno de los archivos PDF está vacío.");

                        if (formFile.Length > _settings.MaxPdfSizeBytes)
                            return BadRequest($"Archivo excede el tamaño permitido: {_settings.MaxPdfSizeBytes}");

                        using var readerStream = formFile.OpenReadStream();
                        using var reader = new PdfReader(readerStream);
                        using var sourcePdf = new PdfDocument(reader);

                        if (sourcePdf.GetNumberOfPages() == 0)
                            return BadRequest("Uno de los archivos PDF está vacío.");

                        merger.Merge(sourcePdf, 1, sourcePdf.GetNumberOfPages());
                    }
                }

                // Concatenado en memoria
                var concatenatedBytes = memStream.ToArray();

                // Optimizar
                var optimizer = new PdfOptimizerLgcy();
                var optimized = optimizer.OptimizarPdf(concatenatedBytes); // 70 = calidad JPEG

                return File(optimized, "application/pdf", "concatenado-optimizado.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }


        [HttpPost("agregar-campos-ocultos")]
            public async Task<IActionResult> AgregarCamposOcultos([FromForm] IFormFile archivo, [FromForm] Dictionary<string, string> campos)
            {
                Inserciones inserc=new Inserciones();
                if (archivo == null || archivo.Length == 0)
                    return BadRequest("Archivo PDF no proporcionado.");

                if (campos != null && campos.Count() != 0)
                {
                    // Convertir archivo a byte[]
                    byte[] archivoBytes;
                    using (var ms = new MemoryStream())
                    {
                        await archivo.CopyToAsync(ms);
                        archivoBytes = ms.ToArray();
                    }

                    // Deserializar el JSON a Dictionary<string, string>
                    
                    // Llamar a la función estática
                    var resultado = inserc.AgregarCamposOcultosPdf(archivoBytes, campos);

                    // Devolver PDF modificado
                    return File(resultado, "application/pdf", "archivo_modificado.pdf");
                }

                return BadRequest("Campos no proporcionados.");
            }
        

        [HttpPost("concatenar")]
        public  IActionResult ConcatenarPdfs(List<IFormFile> files)
        {
            return  ConcatenarOptimizarPdf(files);

            var inicio = DateTime.UtcNow;

            Console.WriteLine("Después de esperar 2 segundos");
            System.Diagnostics.Debug.WriteLine("Instrucción ejecutada en método async");
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (files == null || files.Count == 0)
                return BadRequest("No se recibieron archivos PDF.");

            if (files.Count > _settings.MaxFiles)
                return BadRequest($"Máximo {_settings.MaxFiles} archivos permitidos.");

            var memStream = new MemoryStream();
            _log.Info($"Comienza la concatenación. Cantidad de archivos: {files?.Count ?? 0}");
            try
            {
                using var writer = new PdfWriter(memStream);
                using var outputPdf = new PdfDocument(writer);
                var merger = new PdfMerger(outputPdf);

                foreach (var formFile in files)
                {
                    if (formFile.Length == 0)
                        return BadRequest("Uno de los archivos PDF está vacío.");

                    if (formFile.Length > _settings.MaxPdfSizeBytes)
                    {
                        return BadRequest($"Aarchivo excede el tamaño permitido.{_settings.MaxPdfSizeBytes}");
                    }

                    try
                    {
                        using var readerStream = formFile.OpenReadStream();
                        using var reader = new PdfReader(readerStream);
                        using var sourcePdf = new PdfDocument(reader);

                        if (sourcePdf.GetNumberOfPages() == 0)
                            return BadRequest("Uno de los archivos PDF está vacío.");

                        merger.Merge(sourcePdf, 1, sourcePdf.GetNumberOfPages());
                    }
                    catch (Exception ex)
                    {
                        _log.Error("errores: ", ex);
                        return StatusCode(500, $"Error al procesar un PDF: {ex.Message}");
                    }
                }

                outputPdf.Close();
                /*
                Inserciones inserciones = new Inserciones();
                Dictionary<string,string> dict = new Dictionary<string,string>();
                dict.Add("code", "codigocodigo");
                dict.Add("typeCertDom", "corococo");
                
                var resultBytes = inserciones.AgregarCamposOcultosPdf(memStream.ToArray(),dict);
                resultBytes = inserciones.AgregarCamposOcultosPdf(resultBytes, dict);
                // var resultBase64 = Convert.ToBase64String(resultBytes);
                byte[] p = resultBytes.ToArray();

                using (var ms = new MemoryStream(p))
                using (PdfReader pdfReader = new PdfReader(ms))
                using (var pdfDoc=new PdfDocument(pdfReader))
                {
                    var form = PdfAcroForm.GetAcroForm(pdfDoc,true);
                    var fields = form.GetAllFormFields();
                    foreach(var campo in fields)
                    {
                        string nombre = campo.Key;
                        string valor = campo.Value.GetValueAsString();
                    }    
                
                }
                */
                var fin = DateTime.UtcNow;
                var duracion = fin - inicio;
                _log.Info($"Concatenación exitosa en {duracion.TotalMilliseconds} ms. devolviendo stream");
                return File(memStream.ToArray(), "application/pdf", "concatenado.pdf");
                //return Ok(new
                //{
                //    PdfBase64 = resultBase64,
                //    Message = "PDFs concatenados correctamente"
                //});
            }
            catch (Exception ex)
            {
                _log.Error("Mas errores: ",ex);
                // Loguear (usá tu propio sistema de logs)
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
    }
    public class PdfRequestDto
    {
        public List<string>? PdfsBase64 { get; set; }
    }
    public class ConcatenadorPdfSettings
    {
        public int MaxFiles { get; set; }
        public int MaxPdfSizeBytes { get; set; }
    }
    public class ParametrosVisualizacion
    {
        public string txt { get; set; }
        public int x { get; set; }
        public int y { get; set; }
        public int xt { get; set; }
        public int yt { get; set; }
        public int angulo { get; set; }
        public int tFuente { get; set; }
        public bool opacity { get; set; }
    }
}
