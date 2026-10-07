using AppFileTextProcessor.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;

namespace TextFileProcessor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VsaTextToExcelController : ControllerBase
    {
        private static string BaseDirectory => Environment.GetEnvironmentVariable("FILE_PROCESSOR_DIRECTORY")
            ?? Path.Combine(AppContext.BaseDirectory, "data");
        private const string DefaultInputFileName = "VSA_INIZIALE.txt";
        private const string DefaultOutputFileName = "VSA_FINALE.xlsx";
        private readonly ITextProcessingService _textProcessingService;
        private readonly IExcelExportService _excelExportService;

        public VsaTextToExcelController(ITextProcessingService textProcessingService, IExcelExportService excelExportService)
        {
            _textProcessingService = textProcessingService;
            _excelExportService = excelExportService;
        }

        /// <summary>
        /// Converte un file di testo in un file Excel.
        /// </summary>
        /// <returns>Messaggio di esito dell'elaborazione</returns>
        [HttpPost("process")]
        [SwaggerOperation(Summary = "Converte un file di testo in Excel", Description = "Legge un file di input, lo elabora e genera un file Excel.")]
        [SwaggerResponse(200, "File elaborato e salvato correttamente come 'VSA_FINALE.xlsx'.")]
        [SwaggerResponse(400, "File di input non trovato.")]
        [SwaggerResponse(500, "Errore interno del server.")]
        public IActionResult ProcessLocalFile()
        {
            try
            {
                string inputFilePath = Path.Combine(BaseDirectory, DefaultInputFileName);
                string outputFilePath = Path.Combine(BaseDirectory, DefaultOutputFileName);

                if (!System.IO.File.Exists(inputFilePath))
                {
                    Log.Warning("File di input non trovato: {FilePath}", inputFilePath);
                    return BadRequest("File di input non trovato.");
                }

                string content;
                using (var reader = new StreamReader(inputFilePath, Encoding.GetEncoding("ISO-8859-1")))
                {
                    content = reader.ReadToEnd();
                }

                var processedData = _textProcessingService.ProcessContent(content);
                _excelExportService.SaveToExcel(processedData, outputFilePath);

                Log.Information("File di testo convertito con successo in Excel: {OutputFile}", outputFilePath);
                return Ok("File elaborato e salvato correttamente con il nome 'VSA_FINALE.xlsx'.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Errore durante la conversione del file di testo in Excel.");
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }
    }
}

