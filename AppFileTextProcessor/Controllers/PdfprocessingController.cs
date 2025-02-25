using AppFileTextProcessor.Interface;
using AppFileTextProcessor.Models;
using Microsoft.AspNetCore.Mvc;

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
        /// Sostituisce il logo nel PDF caricato
        /// </summary>
        /// <param name="uploadModel">Il modello contenente il file PDF</param>
        /// <returns>Il PDF modificato con il nuovo logo</returns>
        [HttpPost("replace-logo")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ReplaceLogo([FromForm] PdfUploadModel uploadModel)
        {
            if (uploadModel.File == null || uploadModel.File.Length == 0)
                return BadRequest("Il file PDF è obbligatorio.");

            using var memoryStream = new MemoryStream();
            await uploadModel.File.CopyToAsync(memoryStream);
            byte[] modifiedPdf = _pdfProcessingService.ReplaceLogo(memoryStream.ToArray());

            return File(modifiedPdf, "application/pdf", "modified.pdf");
        }
    }
}
