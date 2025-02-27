using AppFileTextProcessor.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;

namespace TextFileProcessor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppTextConvertController : ControllerBase
    {
        private readonly IAppTextProcessingService _textProcessingService;
        private readonly IComuniService _comuniService;

        public AppTextConvertController(IAppTextProcessingService textProcessingService, IComuniService comuniService)
        {
            _textProcessingService = textProcessingService;
            _comuniService = comuniService;

            // Carica i comuni dai file JSON all'avvio del controller
            _comuniService.LoadComuniFromJson(
                @"C:\Users\Flavio.Simeone\Desktop\PublishedApp\AppFileTextProcessor\json\comuni.json",
                @"C:\Users\Flavio.Simeone\Desktop\PublishedApp\AppFileTextProcessor\json\Elenco-comuni-italiani.json"
            );
        }

        private const string BaseDirectory = @"C:\Users\Flavio.Simeone\Desktop\APPALTO\";
        private const string DefaultInputFileName = "APP_INIZIALE.txt";

        /// <summary>
        /// Elabora il file di testo locale e genera un file di output.
        /// </summary>
        /// <param name="outputFileName">Nome del file di output senza estensione</param>
        /// <returns>Messaggio di esito dell'elaborazione</returns>
        [HttpPost("process")]
        [SwaggerOperation(Summary = "Elabora il file di testo e genera un output", Description = "Legge un file di input, lo elabora e salva il risultato in un nuovo file.")]
        [SwaggerResponse(200, "File elaborato e salvato correttamente.")]
        [SwaggerResponse(400, "Input non valido o file non trovato.")]
        [SwaggerResponse(500, "Errore interno del server.")]
        public async Task<IActionResult> ProcessLocalFile([FromQuery] string outputFileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(outputFileName))
                {
                    Log.Warning("Nome del file di output non fornito.");
                    return BadRequest("I nomi dei file di output sono obbligatori.");
                }

                string inputFilePath = Path.Combine(BaseDirectory, DefaultInputFileName);
                string outputFilePath = Path.Combine(BaseDirectory, outputFileName + ".txt");

                if (!System.IO.File.Exists(inputFilePath))
                {
                    Log.Warning("File di input non trovato: {FilePath}", inputFilePath);
                    return BadRequest("File di input non trovato.");
                }

                string content;
                using (var reader = new StreamReader(inputFilePath, Encoding.UTF8))
                {
                    content = await reader.ReadToEndAsync();
                }

                string processedContent = await _textProcessingService.ProcessContentAsync(content);
                await System.IO.File.WriteAllTextAsync(outputFilePath, processedContent);

                Log.Information("File elaborato e salvato correttamente: {OutputFile}", outputFilePath);
                return Ok("File elaborato e salvato correttamente.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Errore durante l'elaborazione del file.");
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }
    }
}
