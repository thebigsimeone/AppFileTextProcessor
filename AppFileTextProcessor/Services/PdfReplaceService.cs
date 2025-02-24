using AppFileTextProcessor.Interface;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Text.RegularExpressions;

public class PdfReplaceService : IPdfReplaceService
{
    public byte[] ReplaceTextAndImage(byte[] pdfBytes, string oldText, string newText, string imagePath)
    {
        using var inputStream = new MemoryStream(pdfBytes);
        using var outputStream = new MemoryStream();
        var reader = new PdfReader(inputStream);
        var stamper = new PdfStamper(reader, outputStream);

        // Carica il nuovo logo
        Image logo = Image.GetInstance(imagePath);
        logo.ScaleToFit(120, 60);

        for (int i = 1; i <= reader.NumberOfPages; i++)
        {
            PdfContentByte over = stamper.GetOverContent(i);

            // Sostituzione testo
            ReplaceText(reader, stamper, i, oldText, newText);

            // Sostituzione logo (in alto a destra)
            float x = reader.GetPageSize(i).Right - 140;  // Posizione vicino al bordo destro
            float y = reader.GetPageSize(i).Top - 51;     // Posizione in alto
            logo.SetAbsolutePosition(x, y);
            over.AddImage(logo);
        }

        stamper.Close();
        reader.Close();

        return outputStream.ToArray();
    }

    private void ReplaceText(PdfReader reader, PdfStamper stamper, int pageNum, string oldText, string newText)
    {
        PdfDictionary page = reader.GetPageN(pageNum);
        PdfObject obj = page.GetDirectObject(PdfName.CONTENTS);

        if (obj is PRStream stream)
        {
            byte[] data = PdfReader.GetStreamBytes(stream);
            string text = System.Text.Encoding.UTF8.GetString(data);

            if (text.Contains(oldText))
            {
                text = Regex.Replace(text, oldText, newText);
                byte[] newData = System.Text.Encoding.UTF8.GetBytes(text);
                stream.SetData(newData);
            }
        }
    }
}
