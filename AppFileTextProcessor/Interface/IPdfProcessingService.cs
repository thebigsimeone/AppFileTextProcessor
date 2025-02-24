namespace AppFileTextProcessor.Interface
{
    public interface IPdfProcessingService
    {
        byte[] ModifyPdf(byte[] pdfBytes);
    }

}
