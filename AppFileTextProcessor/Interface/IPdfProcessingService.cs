using System;

namespace AppFileTextProcessor.Interface
{
    public interface IPdfProcessingService
    {
        byte[] ReplaceLogo(byte[] pdfData);
    }
}
