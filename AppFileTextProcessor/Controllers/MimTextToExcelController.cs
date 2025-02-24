using AppFileTextProcessor.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace TextFileProcessor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MimTextToExcelController : ControllerBase
    {
        private const string BaseDirectory = @"C:\Users\Flavio.Simeone\Desktop\MIM\";
        private const string DefaultInputFileName = "MIM_INIZIALE.txt";
        private const string DefaultOutputFileName = "MIM_FINALE.xlsx";
        private readonly ITextProcessingService _textProcessingService;
        private readonly IExcelExportService _excelExportService;

        public MimTextToExcelController(ITextProcessingService textProcessingService, IExcelExportService excelExportService)
        {
            _textProcessingService = textProcessingService;
            _excelExportService = excelExportService;
        }

        [HttpPost("process")]
        public IActionResult ProcessLocalFile()
        {
            try
            {
                string inputFilePath = Path.Combine(BaseDirectory, DefaultInputFileName);
                string outputFilePath = Path.Combine(BaseDirectory, DefaultOutputFileName);

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
                _excelExportService.SaveToExcel(processedData, outputFilePath);

                return Ok("File elaborato e salvato correttamente con il nome 'MIM_FINALE.xlsx'.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }
    }
}