using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Text;
using System.Text.RegularExpressions;

namespace TextFileProcessor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VsaTextToExcelController : ControllerBase
    {
        private const string BaseDirectory = @"C:\Users\Utente\Desktop\VSA\";
        private const string DefaultInputFileName = "VSA_INIZIALE.txt";

        [HttpPost("process")]
        public IActionResult ProcessLocalFile([FromQuery] string outputFileName)
        {
            if (string.IsNullOrWhiteSpace(outputFileName))
            {
                return BadRequest("I nomi dei file di output sono obbligatori.");
            }

            string inputFilePath = Path.Combine(BaseDirectory, DefaultInputFileName);
            string outputFilePath = Path.Combine(BaseDirectory, outputFileName + ".xls");

            if (!System.IO.File.Exists(inputFilePath))
            {
                return BadRequest("File di input non trovato.");
            }

            string content;
            using (var reader = new StreamReader(inputFilePath, Encoding.UTF8))
            {
                content = reader.ReadToEnd();
            }

            var processedData = ProcessContent(content);
            SaveToExcel(processedData, outputFilePath);

            return Ok("File elaborato e salvato correttamente.");
        }

        private List<Dictionary<string, string>> ProcessContent(string content)
        {
            var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var processedData = new List<Dictionary<string, string>>();

            Dictionary<string, string> currentRecord = null;
            StringBuilder esitoBuilder = null;

            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();

                if (Regex.IsMatch(trimmedLine, @"^2024\d{7}"))
                {
                    if (currentRecord != null && esitoBuilder != null)
                    {
                        currentRecord["Esito"] = esitoBuilder.ToString().Trim();
                        processedData.Add(currentRecord);
                    }

                    currentRecord = new Dictionary<string, string>
                    {
                        { "Protocollo", trimmedLine.Substring(0, 11) },
                        { "Identificativo", trimmedLine.Substring(12).Replace("'", "") }
                    };
                    esitoBuilder = new StringBuilder();
                }
                else if (currentRecord != null && esitoBuilder != null)
                {
                    esitoBuilder.AppendLine(trimmedLine);
                }
            }

            if (currentRecord != null && esitoBuilder != null)
            {
                currentRecord["Esito"] = esitoBuilder.ToString().Trim();
                processedData.Add(currentRecord);
            }

            return processedData;
        }

        private void SaveToExcel(List<Dictionary<string, string>> data, string outputFilePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Foglio1");

                // Imposta le intestazioni delle colonne secondo il prototipo
                worksheet.Cells[1, 1].Value = "Protocollo";
                worksheet.Cells[1, 2].Value = "Identificativo";
                worksheet.Cells[1, 3].Value = "Esito";

                int row = 2;
                foreach (var record in data)
                {
                    worksheet.Cells[row, 1].Value = record["Protocollo"];
                    worksheet.Cells[row, 2].Value = record["Identificativo"];
                    worksheet.Cells[row, 3].Value = record.ContainsKey("Esito") ? record["Esito"] : string.Empty;

                    row++;
                }

                worksheet.Cells[1, 1, row - 1, 3].Style.Numberformat.Format = "@";

                var fileInfo = new FileInfo(outputFilePath);
                package.SaveAs(fileInfo);
            }
        }
    }
}