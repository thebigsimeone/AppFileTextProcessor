using AppFileTextProcessor.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Text;

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

        [HttpPost("process")]
        public async Task<IActionResult> ProcessLocalFile([FromQuery] string outputFileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(outputFileName))
                {
                    return BadRequest("I nomi dei file di output sono obbligatori.");
                }

                string inputFilePath = Path.Combine(BaseDirectory, DefaultInputFileName);
                string outputFilePath = Path.Combine(BaseDirectory, outputFileName + ".txt");

                if (!System.IO.File.Exists(inputFilePath))
                {
                    return BadRequest("File di input non trovato.");
                }

                string content;
                using (var reader = new StreamReader(inputFilePath, Encoding.UTF8))
                {
                    content = await reader.ReadToEndAsync();
                }

                string processedContent = await _textProcessingService.ProcessContentAsync(content);
                await System.IO.File.WriteAllTextAsync(outputFilePath, processedContent);

                return Ok("File elaborato e salvato correttamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore: {ex.Message}");
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }
    }
}
