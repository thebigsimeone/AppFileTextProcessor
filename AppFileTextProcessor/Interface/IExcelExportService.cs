namespace AppFileTextProcessor.Interface
{
    public interface IExcelExportService
    {
        void SaveToExcel(List<(string Protocollo, string Identificativo, string Esito)> data, string outputFilePath);
    }
}
