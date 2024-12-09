using AppFileTextProcessor.Interface;
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
        private const string DefaultOutputFileName = "VSA_FINALE.xlsx";
        private readonly ITextProcessingService _textProcessingService;
        private readonly IExcelExportService _excelExportService;

        public VsaTextToExcelController(ITextProcessingService textProcessingService, IExcelExportService excelExportService)
        {
            _textProcessingService = textProcessingService;
            _excelExportService = excelExportService;
        }

        [HttpPost("process")]
        public IActionResult ProcessLocalFile()
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

            return Ok("File elaborato e salvato correttamente con il nome 'VSA_FINALE.xlsx'.");
        }
    }
}
