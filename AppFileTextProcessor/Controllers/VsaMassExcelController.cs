using AppFileTextProcessor.Interface;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class VsaMassExcelController : ControllerBase
{
    private const string BaseDirectory = @"C:\Users\Utente\Desktop\VSA\";
    private const string DefaultInputFileName = "VSA_MASS.xlsx";
    private const string DefaultOutputFileName = "VSA_MASS_FINALE.xlsx";

    private readonly IExcelProcessingMassService _excelProcessingMassService;

    public VsaMassExcelController(IExcelProcessingMassService excelProcessingMassService)
    {
        _excelProcessingMassService = excelProcessingMassService;
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

        _excelProcessingMassService.ProcessAndSaveExcel(inputFilePath, outputFilePath);

        return Ok($"File elaborato e salvato correttamente con il nome '{DefaultOutputFileName}'.");
    }
}
