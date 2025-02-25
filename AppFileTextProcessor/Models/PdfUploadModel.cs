using System.ComponentModel.DataAnnotations;

namespace AppFileTextProcessor.Models
{
    public class PdfUploadModel
    {
        [Required]
        public IFormFile? File { get; set; }
    }
}
