namespace AppFileTextProcessor.Interface
{
    public interface ITextProcessingService
    {
        List<(string Protocollo, string Identificativo, string Esito)> ProcessContent(string content);
    }
}
