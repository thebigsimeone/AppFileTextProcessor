using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Text;
using System.Text.RegularExpressions;

namespace TextFileProcessor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MimTextToExcelController : ControllerBase
    {

        /*        [HttpPost("process")]
        public IActionResult ProcessLocalFile([FromQuery] string inputFilePath, [FromQuery] string outputFilePath)
        {
            if (string.IsNullOrWhiteSpace(inputFilePath) || string.IsNullOrWhiteSpace(outputFilePath))
            {
                return BadRequest("I percorsi dei file di input e output sono obbligatori.");
            }

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
        }*/
        private const string BaseDirectory = @"C:\Users\Utente\Desktop\MIM\";
        private const string DefaultInputFileName = "MIM_INIZIALE.txt";

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

        private List<(string Protocollo, string Identificativo, string Esito)> ProcessContent(string content)
        {
            var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var processedData = new List<(string Protocollo, string Identificativo, string Esito)>();

            string protocollo = null;
            string identificativo = null;
            StringBuilder esito = new StringBuilder();
            bool newRecord = false;

            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();

                // Riconoscere la riga contenente il Protocollo
                if (Regex.IsMatch(trimmedLine, @"^2024\d{7}\s[A-Z0-9]{11,16}$"))
                {
                    if (newRecord)
                    {
                        processedData.Add((protocollo, identificativo, esito.ToString().Trim()));
                        esito.Clear();
                    }

                    protocollo = trimmedLine.Substring(0, 11);
                    identificativo = trimmedLine.Substring(12);
                    newRecord = true;
                }
                else
                {
                    if (newRecord)
                    {
                        esito.AppendLine(trimmedLine);
                    }
                }
            }

            if (newRecord)
            {
                processedData.Add((protocollo, identificativo, esito.ToString().Trim()));
            }

            return processedData;
        }

        private void SaveToExcel(List<(string Protocollo, string Identificativo, string Esito)> data, string outputFilePath)
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

