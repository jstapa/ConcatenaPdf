using SkiaSharp;
using iTextSharp.text.pdf;
namespace Concatena.Pdf2
{
    public class PdfOptimizerLgcy
    {
        public byte[] OptimizarPdf(byte[] archivo, int calidad = 75)
        {
            using (var reader = new PdfReader(archivo))
            using (var ms = new MemoryStream())
            {
                int n = reader.XrefSize;
                for (int i = 0; i < n; i++)
                {
                    var obj = reader.GetPdfObject(i);
                    if (obj == null || !obj.IsStream()) continue;

                    var dict = (PdfDictionary)PdfReader.GetPdfObject(obj);
                    var subType = dict.GetAsName(PdfName.SUBTYPE);
                    if (!PdfName.IMAGE.Equals(subType)) continue;

                    var stream = (PRStream)obj;
                    var bytes = PdfReader.GetStreamBytesRaw(stream);

                    // Intentar decodificar imagen con Skia
                    using var codec = SKCodec.Create(new SKMemoryStream(bytes));
                    if (codec == null) continue;

                    var bmp = SKBitmap.Decode(codec);
                    if (bmp == null) continue;

                    using var image = SKImage.FromBitmap(bmp);
                    using var data = image.Encode(SKEncodedImageFormat.Jpeg, calidad);

                    // Reemplazar contenido del stream
                    stream.SetData(data.ToArray(), false, PRStream.BEST_COMPRESSION);
                    stream.Put(PdfName.FILTER, PdfName.DCTDECODE);
                }

                using (var stamper = new PdfStamper(reader, ms))
                {
                    stamper.Writer.CompressionLevel = 9;
                    stamper.SetFullCompression();
                }

                return ms.ToArray();
            }
        }
    }
    }
