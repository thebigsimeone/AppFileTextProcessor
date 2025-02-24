using AppFileTextProcessor.Interface;

public class PdfProcessingService : IPdfProcessingService
{
    private readonly IPdfReplaceService _pdfReplaceService;
    private readonly string _logoPath = Path.Combine(Directory.GetCurrentDirectory(), "logo", "logo-eurocredit.png");

    public PdfProcessingService(IPdfReplaceService pdfReplaceService)
    {
        _pdfReplaceService = pdfReplaceService;
    }

    public byte[] ModifyPdf(byte[] pdfBytes)
    {
        return _pdfReplaceService.ReplaceTextAndImage(pdfBytes, "Coface", "Eurocredit", _logoPath);
    }
}
