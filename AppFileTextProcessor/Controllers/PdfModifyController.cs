using AppFileTextProcessor.Interface;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class PdfModifyController : ControllerBase
{
    private readonly IPdfProcessingService _pdfProcessingService;

    public PdfModifyController(IPdfProcessingService pdfProcessingService)
    {
        _pdfProcessingService = pdfProcessingService;
    }

    [HttpPost("modify")]
    public async Task<IActionResult> ModifyPdf(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File non valido o vuoto.");

        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            byte[] modifiedPdf = _pdfProcessingService.ModifyPdf(stream.ToArray());

            return File(modifiedPdf, "application/pdf", "Modified_" + file.FileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Errore interno: {ex.Message}");
        }
    }
}
