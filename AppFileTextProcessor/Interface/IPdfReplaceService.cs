namespace AppFileTextProcessor.Interface
{
    public interface IPdfReplaceService
    {
        byte[] ReplaceTextAndImage(byte[] pdfBytes, string oldText, string newText, string imagePath);
    }

}
