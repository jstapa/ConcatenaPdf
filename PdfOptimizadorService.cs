using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Xobject;
using iText.Kernel.Pdf.Filters;
using iText.Kernel.Utils;

public class PdfOptimizer
{


    public async Task<byte[]> ConcatenarYOptimizarAsync(IList<byte[]> pdfsBytes, long calidad = 85)
    {
        if (pdfsBytes == null || pdfsBytes.Count == 0)
            throw new ArgumentException("La lista de PDFs está vacía");

        byte[] pdfConcatenado;

        // Concatenar PDFs
        using (var outputStream = new MemoryStream())
        {
            var writer = new PdfWriter(outputStream);
            var pdfDest = new PdfDocument(writer);
            var merger = new PdfMerger(pdfDest);

            foreach (var pdfBytes in pdfsBytes)
            {
                using var ms = new MemoryStream(pdfBytes);
                using var pdfSrc = new PdfDocument(new PdfReader(ms));
                merger.Merge(pdfSrc, 1, pdfSrc.GetNumberOfPages());
            }

            pdfDest.Close(); // importante para cerrar correctamente
            pdfConcatenado = outputStream.ToArray();
        }

        // Optimizar PDF
        var pdfOptimizado =  OptimizarPdfAsync(pdfConcatenado, calidad);

        return pdfOptimizado;
    }
    public byte[] OptimizarPdfAsync(byte[] inputPdf, long calidad)
    {
        using var outputStream = new MemoryStream();
        using var pdfReader = new PdfReader(new MemoryStream(inputPdf));
        using var pdfWriter = new PdfWriter(outputStream, new WriterProperties().SetFullCompressionMode(true));
        using var pdfDoc = new PdfDocument(pdfReader, pdfWriter);

        for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
        {
            var page = pdfDoc.GetPage(i);
            var resources = page.GetResources();

            foreach (var xObjectName in resources.GetResourceNames(PdfName.XObject))
            {
                var xObject = resources.GetResource(xObjectName);
                if (xObject ==null || !xObject.IsStream()) continue;

                var stream = (PdfStream)xObject;
                var subtype = stream.GetAsName(PdfName.Subtype);
                if (subtype != null && subtype.Equals(PdfName.Image))
                {
                    var imageBytes = stream.GetBytes();
                    using var msImg = new MemoryStream(imageBytes);

                    try
                    {
                        using var img = Image.FromStream(msImg);
                        using var compressedStream = new MemoryStream();

                        var codec = GetJpegEncoder();
                        var encoderParams = new EncoderParameters(1);
                        encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, calidad);

                        img.Save(compressedStream, codec, encoderParams);

                        stream.SetData(compressedStream.ToArray());
                        stream.Put(PdfName.Filter, PdfName.DCTDecode);
                    }
                    catch
                    {
                        // Si la imagen no se puede procesar, la dejamos como está
                    }
                }
            }
        }

        pdfDoc.Close(); // Asegura cierre asincrónico en .NET 6+
        return outputStream.ToArray();
    }
    public static byte[] OptimizarPdf(byte[] inputPdf, long calidad = 75)
    {
        using var outputStream = new MemoryStream();
        var reader = new PdfReader(new MemoryStream(inputPdf));
        var writer = new PdfWriter(outputStream, new WriterProperties().SetFullCompressionMode(true));
        var pdfDoc = new PdfDocument(reader, writer);

        for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
        {
            var page = pdfDoc.GetPage(i);
            var resources = page.GetResources();

            foreach (var name in resources.GetResourceNames(iText.Kernel.Pdf.PdfName.XObject))
            {
                var xObj = resources.GetResource(name);
                if (xObj == null || !xObj.IsStream()) continue;

                var stream = (PdfStream)xObj;
                var subtype = stream.GetAsName(iText.Kernel.Pdf.PdfName.Subtype);
                if (subtype != null && subtype.Equals(iText.Kernel.Pdf.PdfName.Image))
                {
                    try
                    {
                        var imageBytes = stream.GetBytes();
                        using var msImg = new MemoryStream(imageBytes);
                        using var img = System.Drawing.Image.FromStream(msImg);
                        using var compressedStream = new MemoryStream();

                        var codec = GetJpegEncoder();
                        var encoderParams = new EncoderParameters(1);
                        encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, calidad);

                        img.Save(compressedStream, codec, encoderParams);

                        stream.SetData(compressedStream.ToArray());
                        stream.Put(iText.Kernel.Pdf.PdfName.Filter, iText.Kernel.Pdf.PdfName.DCTDecode);
                    }
                    catch
                    {
                        // Si no se puede procesar la imagen, la dejamos como está
                    }
                }
            }
        }

        pdfDoc.Close();
        return outputStream.ToArray();
    }

    private static ImageCodecInfo GetJpegEncoder()
    {
        return ImageCodecInfo.GetImageDecoders()
            .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
    }
}
