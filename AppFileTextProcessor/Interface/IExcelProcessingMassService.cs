namespace AppFileTextProcessor.Interface
{
    public interface IExcelProcessingMassService
    {
        List<(string Protocollo, string Identificativo, string Esito)> ProcessExcel(string inputFilePath);
        void ProcessAndSaveExcel(string inputFilePath, string outputFilePath);
    }
}
