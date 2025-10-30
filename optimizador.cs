public class PdfOptimizerService
{
    public async Task<byte[]> OptimizarPdfAsync(byte[] archivo)
    {
        using var inputStream = new MemoryStream(archivo);
        using var outputStream = new MemoryStream();

        var reader = new iText.Kernel.Pdf.PdfReader(inputStream);
        var writer = new iText.Kernel.Pdf.PdfWriter(outputStream, new iText.Kernel.Pdf.WriterProperties()
            .SetCompressionLevel(9)
            .SetFullCompressionMode(true));

        var pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader, writer);

        for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
        {
            var page = pdfDoc.GetPage(i);
            var resources = page.GetResources();
            var xObjects = resources.GetResource(iText.Kernel.Pdf.PdfName.XObject);

            if (xObjects == null) continue;

            foreach (var entry in xObjects.KeySet())
            {
                var obj = xObjects.GetAsStream(entry);
                if (obj == null || !obj.GetAsName(iText.Kernel.Pdf.PdfName.Subtype).Equals(iText.Kernel.Pdf.PdfName.Image))
                    continue;

                try
                {
                    var imageBytes = obj.GetBytes();
                    using var imageStream = new MemoryStream(imageBytes);
                    using var bitmap = new System.Drawing.Bitmap(imageStream);

                    var codec = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.MimeType == "image/jpeg");
                    var encoderParams = new System.Drawing.Imaging.EncoderParameters(1);
                    encoderParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 85L);

                    using var optimizedImageStream = new MemoryStream();
                    bitmap.Save(optimizedImageStream, codec, encoderParams);
                    var optimizedBytes = optimizedImageStream.ToArray();

                    var newImage = new iText.IO.Image.ImageDataFactory().Create(optimizedBytes);
                    var newImageXObject = new iText.Kernel.Pdf.Xobject.PdfImageXObject(newImage);
                    xObjects.Put(entry, newImageXObject.GetPdfObject());
                }
                catch { }
            }
        }

        pdfDoc.Close();

        return outputStream.ToArray();
    }
	using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using iText.IO.Image;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

public class PdfProcessorService
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
        var pdfOptimizado = await OptimizarPdfAsync(pdfConcatenado, calidad);

        return pdfOptimizado;
    }

    private async Task<byte[]> OptimizarPdfAsync(byte[] inputPdf, long calidad)
    {
        using var outputStream = new MemoryStream();
        using var pdfReader = new PdfReader(new MemoryStream(inputPdf));
        using var pdfWriter = new PdfWriter(outputStream, new WriterProperties().SetFullCompressionMode(true));
        using var pdfDoc = new PdfDocument(pdfReader, pdfWriter);

        for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
        {
            var page = pdfDoc.GetPage(i);
            var resources = page.GetResources();

            foreach (var xObjectName in resources.GetResourceNames(iText.Kernel.Pdf.PdfName.XObject))
            {
                var xObject = resources.GetResource(iText.Kernel.Pdf.PdfName.XObject, xObjectName);
                if (xObject.IsStream())
                {
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
                            // si falla la imagen, la dejamos como está
                        }
                    }
                }
            }
        }

        pdfDoc.Close();
        return outputStream.ToArray();
    }

    private ImageCodecInfo GetJpegEncoder()
    {
        return ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
    }
}

}
