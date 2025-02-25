using AppFileTextProcessor.Interface;
using iText.IO.Image;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;
using System.IO;

namespace AppFileTextProcessor.Services
{
    public class PdfProcessingService : IPdfProcessingService
    {
        private const string LogoPath = "logo/logo-eurocredit.png";

        public byte[] ReplaceLogo(byte[] pdfData)
        {
            using var inputStream = new MemoryStream(pdfData);
            using var outputStream = new MemoryStream();
            using var pdfReader = new PdfReader(inputStream);
            using var pdfWriter = new PdfWriter(outputStream);
            using var pdfDocument = new PdfDocument(pdfReader, pdfWriter);
            var document = new Document(pdfDocument);

            ImageData newLogo = ImageDataFactory.Create(LogoPath);
            Image logoImage = new Image(newLogo);

            // Recupera dimensioni originali
            float originalWidth = logoImage.GetImageWidth();
            float originalHeight = logoImage.GetImageHeight();

            // Definisce la larghezza massima in base alla pagina
            float maxLogoWidth = 150;  // Larghezza massima consentita
            float scaleFactor = maxLogoWidth / originalWidth;

            // Applica la scala uniforme per mantenere le proporzioni
            logoImage.Scale(scaleFactor, scaleFactor);

            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                var page = pdfDocument.GetPage(i);
                float x = page.GetPageSize().GetWidth() - logoImage.GetImageScaledWidth() - 25; // Posizione X
                float y = page.GetPageSize().GetHeight() - logoImage.GetImageScaledHeight() - 25; // Posizione Y

                var canvas = new Canvas(new PdfCanvas(page), page.GetPageSize());
                canvas.Add(logoImage.SetFixedPosition(i, x, y));
            }

            document.Close();
            return outputStream.ToArray();
        }
    }
}
