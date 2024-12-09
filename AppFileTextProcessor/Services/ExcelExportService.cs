using AppFileTextProcessor.Interface;
using OfficeOpenXml;

namespace AppFileTextProcessor.Services
{
    public class ExcelExportService : IExcelExportService
    {
        public void SaveToExcel(List<(string Protocollo, string Identificativo, string Esito)> data, string outputFilePath)
        {
            // Imposta il contesto della licenza di EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Foglio1");

                worksheet.Cells[1, 1].Value = "Protocollo";
                worksheet.Cells[1, 2].Value = "Identificativo";
                worksheet.Cells[1, 3].Value = "ESITO";

                for (int i = 0; i < data.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = data[i].Protocollo;
                    worksheet.Cells[i + 2, 2].Value = data[i].Identificativo;
                    worksheet.Cells[i + 2, 3].Value = data[i].Esito;
                }

                // Imposta la formattazione delle celle come testo
                worksheet.Cells[1, 1, data.Count + 1, 3].Style.Numberformat.Format = "@";

                var fileInfo = new FileInfo(outputFilePath);
                package.SaveAs(fileInfo);
            }
        }
    }
}
