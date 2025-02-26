using AppFileTextProcessor.Interface;
using AppFileTextProcessor.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;

namespace AppFileTextProcessor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PdfProcessingController : ControllerBase
    {
        private readonly IPdfProcessingService _pdfProcessingService;

        public PdfProcessingController(IPdfProcessingService pdfProcessingService)
        {
            _pdfProcessingService = pdfProcessingService;
        }

        /// <summary>
        /// Sostituisce il logo nel PDF caricato.
        /// </summary>
        /// <param name="uploadModel">Il modello contenente il file PDF</param>
        /// <returns>Il PDF modificato con il nuovo logo</returns>
        [HttpPost("replace-logo")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(Summary = "Sostituisce il logo nel PDF", Description = "Carica un file PDF e sostituisce il logo con uno nuovo.")]
        [SwaggerResponse(200, "PDF elaborato con successo e restituito.")]
        [SwaggerResponse(400, "Il file PDF è obbligatorio.")]
        [SwaggerResponse(500, "Errore interno del server.")]
        public async Task<IActionResult> ReplaceLogo([FromForm] PdfUploadModel uploadModel)
        {
            if (uploadModel.File == null || uploadModel.File.Length == 0)
            {
                Log.Warning("Tentativo di upload senza file valido.");
                return BadRequest("Il file PDF è obbligatorio.");
            }

            try
            {
                Log.Information("Ricevuto file PDF: {FileName}, Dimensione: {FileSize} bytes", uploadModel.File.FileName, uploadModel.File.Length);

                using var memoryStream = new MemoryStream();
                await uploadModel.File.CopyToAsync(memoryStream);
                byte[] modifiedPdf = _pdfProcessingService.ReplaceLogo(memoryStream.ToArray());

                Log.Information("Sostituzione logo completata con successo per il file: {FileName}", uploadModel.File.FileName);
                return File(modifiedPdf, "application/pdf", "modified.pdf");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Errore durante la sostituzione del logo nel file PDF: {FileName}", uploadModel.File.FileName);
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }
    }
}
