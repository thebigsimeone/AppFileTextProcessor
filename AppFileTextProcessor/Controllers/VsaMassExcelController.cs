using AppFileTextProcessor.Interface;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;

namespace TextFileProcessor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VsaMassExcelController : ControllerBase
    {
        private const string BaseDirectory = @"C:\Users\Flavio.Simeone\Desktop\VSA\";
        private const string DefaultInputFileName = "VSA_MASS.xlsx";
        private const string DefaultOutputFileName = "VSA_MASS_FINALE.xlsx";

        private readonly IExcelProcessingMassService _excelProcessingMassService;

        public VsaMassExcelController(IExcelProcessingMassService excelProcessingMassService)
        {
            _excelProcessingMassService = excelProcessingMassService;
        }

        /// <summary>
        /// Elabora un file Excel e genera un file di output.
        /// </summary>
        /// <returns>Messaggio di esito dell'elaborazione</returns>
        [HttpPost("process")]
        [SwaggerOperation(Summary = "Elabora un file Excel e genera un output", Description = "Legge un file Excel, lo elabora e salva il risultato in un nuovo file.")]
        [SwaggerResponse(200, "File elaborato e salvato correttamente.")]
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

                _excelProcessingMassService.ProcessAndSaveExcel(inputFilePath, outputFilePath);

                Log.Information("File Excel elaborato con successo: {OutputFile}", outputFilePath);
                return Ok($"File elaborato e salvato correttamente con il nome '{DefaultOutputFileName}'.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Errore durante l'elaborazione del file Excel.");
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }
    }
}
