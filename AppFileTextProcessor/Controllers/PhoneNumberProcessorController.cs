using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;

namespace AppFileTextProcessor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhoneNumberProcessorController : ControllerBase
    {
        private const string BaseDirectory = @"C:\Users\Flavio.Simeone\Desktop\MA7_EUROSTA\";

        [HttpPost("process")]
        public IActionResult ProcessPhoneNumbers([FromQuery] string inputFileName, [FromQuery] string outputFileName)
        {
            if (string.IsNullOrWhiteSpace(inputFileName) || string.IsNullOrWhiteSpace(outputFileName))
            {
                return BadRequest("I nomi dei file di input e output sono obbligatori.");
            }

            string inputFilePath = Path.Combine(BaseDirectory, inputFileName + ".xlsx");
            string outputFilePath = Path.Combine(BaseDirectory, outputFileName + ".xls");

            if (!System.IO.File.Exists(inputFilePath))
            {
                return BadRequest("File di input non trovato.");
            }

            try
            {
                ProcessExcelFile(inputFilePath, outputFilePath);
                return Ok("File elaborato e salvato correttamente.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Errore durante l'elaborazione del file: {ex.Message}");
            }
        }

        private void ProcessExcelFile(string inputFilePath, string outputFilePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(new FileInfo(inputFilePath)))
            {
                var worksheet = package.Workbook.Worksheets["Foglio1"]; // Accedi al foglio "Foglio1"

                if (worksheet == null)
                {
                    throw new Exception("Il foglio di lavoro 'Foglio1' non esiste nel file Excel.");
                }

                for (int row = 2; row <= worksheet.Dimension.End.Row; row++) // Partendo dalla riga 2 per saltare l'intestazione
                {
                    var phoneNumbers = worksheet.Cells[row, 22].Text?.TrimStart(); // Colonna S (19-esima colonna)

                    if (string.IsNullOrEmpty(phoneNumbers) || phoneNumbers.Contains("NON RISALIBILE"))
                    {
                        worksheet.Cells[row, 22].Value = "NON RISALIBILE";
                        worksheet.Cells[row, 23].Value = null;
                        worksheet.Cells[row, 24].Value = null;
                        worksheet.Cells[row, 25].Value = null;
                        continue;
                    }

                    var phones = SplitPhoneNumbers(phoneNumbers);

                    worksheet.Cells[row, 22].Value = phones.Item1; // Linea fissa (Telefono cedente)
                    worksheet.Cells[row, 23].Value = phones.Item2; // Linea fissa (Telefono alternativo cedente)
                    worksheet.Cells[row, 24].Value = phones.Item3; // Cellulare primario (Cellulare cedente)
                    worksheet.Cells[row, 25].Value = phones.Item4; // Cellulare alternativo (Cellulare alternativo cedente)
                }

                // Imposta la formattazione delle celle come testo
                worksheet.Cells[2, 22, worksheet.Dimension.End.Row, 25].Style.Numberformat.Format = "@";

                package.SaveAs(new FileInfo(outputFilePath));
            }
        }

        private (string, string, string, string) SplitPhoneNumbers(string phoneNumbers)
        {
            var phones = phoneNumbers.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Distinct()
                                     .ToList();
            string landline1 = null;
            string landline2 = null;
            string mobile1 = null;
            string mobile2 = null;

            foreach (var phone in phones)
            {
                if (phone.StartsWith("3"))
                {
                    if (mobile1 == null)
                    {
                        mobile1 = phone;
                    }
                    else
                    {
                        mobile2 = phone;
                    }
                }
                else if (phone.StartsWith("0"))
                {
                    if (landline1 == null)
                    {
                        landline1 = phone;
                    }
                    else
                    {
                        landline2 = phone;
                    }
                }
            }

            return (landline1, landline2, mobile1, mobile2);
        }
    }
}