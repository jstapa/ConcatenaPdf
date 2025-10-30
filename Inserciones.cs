using iText.Forms;
using iText.Forms.Fields;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Annot;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Extgstate;
using System;
using System.Collections.Generic;
using System.IO;


 
namespace Concatena.Pdf2
{
    public class Inserciones
    {


        public  byte[] AgregarCamposOcultosPdf(byte[] archivoOriginal, Dictionary<string, string> campos)
        {
            using var output = new MemoryStream();
            using var reader = new PdfReader(new MemoryStream(archivoOriginal));
            using var writer = new PdfWriter(output);
            using var pdfDoc = new PdfDocument(reader, writer);

            var form = PdfAcroForm.GetAcroForm(pdfDoc, true);
            var page = pdfDoc.GetFirstPage();
             
            float y = 10;

            foreach (var kv in campos)
            {
                var rect = new Rectangle(10, y, 200, 20);
                 
                var field2 = new TextFormFieldBuilder(pdfDoc,kv.Key).CreateText();

                field2.SetValue(kv.Value);
                
                var builderd = new TextFormFieldBuilder(pdfDoc, kv.Key);
                builderd.SetWidgetRectangle(rect);

                var builder=builderd.SetPage(page).CreateText();

                builder.SetValue(kv.Value);
                

                //var field = builder.CreateText();
                //field.SetValue(kv.Value);
                // field.
                // Establecer el campo como oculto
                //field.SetVisibility(PdfTextFormField..HIDDEN);
                form.AddField(builder, page);
               // form.AddField(builder, page);
                y += 25;
            }

            form.FlattenFields(); // Opcional: convierte campos en contenido fijo
            
            pdfDoc.Close();
            return output.ToArray();
        }


        public async Task<byte[]> InsertarTextoImagenPdfXY(byte[] mipdf, string texto, int vG, bool opacity, int vX, int vY, int tFuente)
        {
            using var inputStream = new MemoryStream(mipdf);
            using var outputStream = new MemoryStream();

            var pdfReader = new PdfReader(inputStream);
            var pdfWriter = new PdfWriter(outputStream);
            var pdfDoc = new PdfDocument(pdfReader, pdfWriter);
            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            int numPages = pdfDoc.GetNumberOfPages();

            for (int i = 1; i <= numPages; i++)
            {
                var page = pdfDoc.GetPage(i);
                var canvas = new PdfCanvas(page.NewContentStreamAfter(), page.GetResources(), pdfDoc);

                if (opacity)
                {
                    var gs = new PdfExtGState().SetFillOpacity(0.3f);
                    canvas.SaveState();
                    canvas.SetExtGState(gs);
                }

                string textoPagina = string.Format(texto, i);

                canvas.BeginText();
                canvas.SetFontAndSize(font, tFuente);
                //canvas.MoveText(vX, vY);

                switch (vG)
                {
                    case 0: // izquierda
                        canvas.ShowText(textoPagina);
                        break;
                    case 90: // rotado 90º
                        canvas.SetTextMatrix(0, 1, -1, 0, vX, vY);
                        canvas.ShowText(textoPagina);
                        break;
                    case 180: // rotado 180º
                        canvas.SetTextMatrix(-1, 0, 0, -1, vX, vY);
                        canvas.ShowText(textoPagina);
                        break;
                    case 270: // rotado 270º
                        canvas.SetTextMatrix(0, -1, 1, 0, vX, vY);
                        canvas.ShowText(textoPagina);
                        break;
                    default: // centro (0º)
                        canvas.ShowText(textoPagina);
                        break;
                }

                canvas.EndText();

                if (opacity)
                {
                    canvas.RestoreState();
                }
            }

            pdfDoc.Close();
            return outputStream.ToArray();
        }

    }
}
