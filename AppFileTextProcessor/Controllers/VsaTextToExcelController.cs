using AppFileTextProcessor.Interface;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Text;

namespace TextFileProcessor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VsaTextToExcelController : ControllerBase
    {
        private const string BaseDirectory = @"C:\Users\Utente\Desktop\VSA\";
        private const string DefaultInputFileName = "VSA_INIZIALE.txt";
        private readonly ITextProcessingService _textProcessingService;

        public VsaTextToExcelController(ITextProcessingService textProcessingService)
        {
            _textProcessingService = textProcessingService;
        }

        [HttpPost("process")]
        public IActionResult ProcessLocalFile([FromQuery] string outputFileName)
        {
            if (string.IsNullOrWhiteSpace(outputFileName))
            {
                return BadRequest("I nomi dei file di output sono obbligatori.");
            }

            string inputFilePath = Path.Combine(BaseDirectory, DefaultInputFileName);
            string outputFilePath = Path.Combine(BaseDirectory, outputFileName + ".xlsx");

            if (!System.IO.File.Exists(inputFilePath))
            {
                return BadRequest("File di input non trovato.");
            }

            string content;
            using (var reader = new StreamReader(inputFilePath, Encoding.GetEncoding("ISO-8859-1")))
            {
                content = reader.ReadToEnd();
            }

            var processedData = _textProcessingService.ProcessContent(content);

            if (processedData == null || processedData.Count == 0)
            {
                return BadRequest("Nessun dato trovato nel file di input.");
            }

            SaveToExcel(processedData, outputFilePath);

            return Ok("File elaborato e salvato correttamente.");
        }

        private void SaveToExcel(List<(string Protocollo, string Identificativo, string Esito)> data, string outputFilePath)
        {
            // Imposta il contesto della licenza di EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Foglio1");

                // Imposta i titoli delle colonne
                worksheet.Cells[1, 1].Value = "Protocollo";
                worksheet.Cells[1, 2].Value = "Identificativo";
                worksheet.Cells[1, 3].Value = "ESITO";

                // Popola il foglio di lavoro con i dati processati
                for (int i = 0; i < data.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = data[i].Protocollo;
                    worksheet.Cells[i + 2, 2].Value = "'" + data[i].Identificativo; // Mantenere gli zeri iniziali per l'identificativo
                    worksheet.Cells[i + 2, 3].Value = data[i].Esito;
                }

                // Imposta la formattazione delle celle come testo per mantenere la consistenza dei dati
                worksheet.Cells[1, 1, data.Count + 1, 3].Style.Numberformat.Format = "@";
                worksheet.Cells.AutoFitColumns(); // Adatta automaticamente le colonne al contenuto

                var fileInfo = new FileInfo(outputFilePath);
                package.SaveAs(fileInfo);
            }
        }
    }
}