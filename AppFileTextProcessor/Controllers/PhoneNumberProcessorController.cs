using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;

namespace AppFileTextProcessor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhoneNumberProcessorController : ControllerBase
    {
        private const string BaseDirectory = @"C:\Users\Flavio.Simeone\Desktop\MA7_EUROSTA\";

        /// <summary>
        /// Elabora un file Excel e suddivide i numeri di telefono in colonne specifiche.
        /// </summary>
        /// <param name="inputFileName">Nome del file Excel di input (senza estensione)</param>
        /// <param name="outputFileName">Nome del file Excel di output (senza estensione)</param>
        /// <returns>Messaggio di esito dell'elaborazione</returns>
        [HttpPost("process")]
        [SwaggerOperation(Summary = "Elabora un file Excel per suddividere i numeri di telefono", Description = "Legge un file Excel, analizza i numeri di telefono e li suddivide in più colonne.")]
        [SwaggerResponse(200, "File elaborato e salvato correttamente.")]
        [SwaggerResponse(400, "I nomi dei file di input e output sono obbligatori.")]
        [SwaggerResponse(404, "File di input non trovato.")]
        [SwaggerResponse(500, "Errore interno del server.")]
        public IActionResult ProcessPhoneNumbers([FromQuery] string inputFileName, [FromQuery] string outputFileName)
        {
            if (string.IsNullOrWhiteSpace(inputFileName) || string.IsNullOrWhiteSpace(outputFileName))
            {
                Log.Warning("Nome file non valido: input = {InputFile}, output = {OutputFile}", inputFileName, outputFileName);
                return BadRequest("I nomi dei file di input e output sono obbligatori.");
            }

            string inputFilePath = Path.Combine(BaseDirectory, inputFileName + ".xlsx");
            string outputFilePath = Path.Combine(BaseDirectory, outputFileName + ".xls");

            if (!System.IO.File.Exists(inputFilePath))
            {
                Log.Warning("File di input non trovato: {FilePath}", inputFilePath);
                return NotFound("File di input non trovato.");
            }

            try
            {
                ProcessExcelFile(inputFilePath, outputFilePath);
                Log.Information("File elaborato con successo: {OutputFilePath}", outputFilePath);
                return Ok("File elaborato e salvato correttamente.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Errore durante l'elaborazione del file: {FilePath}", inputFilePath);
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
                    Log.Warning("Il foglio di lavoro 'Foglio1' non esiste nel file Excel: {FilePath}", inputFilePath);
                    throw new Exception("Il foglio di lavoro 'Foglio1' non esiste nel file Excel.");
                }

                int totalRows = worksheet.Dimension.End.Row;
                Log.Information("Elaborazione di {TotalRows} righe nel file: {FilePath}", totalRows, inputFilePath);

                for (int row = 2; row <= totalRows; row++) // Partendo dalla riga 2 per saltare l'intestazione
                {
                    var phoneNumbers = worksheet.Cells[row, 22].Text?.TrimStart(); // Colonna S (22-esima colonna)

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
                worksheet.Cells[2, 22, totalRows, 25].Style.Numberformat.Format = "@";

                package.SaveAs(new FileInfo(outputFilePath));
                Log.Information("File Excel elaborato e salvato: {OutputFilePath}", outputFilePath);
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
