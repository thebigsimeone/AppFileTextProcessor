using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;

namespace AppFileTextProcessor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExcelMergerController : ControllerBase
    {
        [HttpPost("merge")]
        public async Task<IActionResult> MergeExcelFiles(IFormFile file1, IFormFile file2)
        {
            if (file1 == null || file2 == null)
            {
                return BadRequest("Both files are required.");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package1 = new ExcelPackage(file1.OpenReadStream()))
            using (var package2 = new ExcelPackage(file2.OpenReadStream()))
            using (var outputPackage = new ExcelPackage())
            {
                var sheet1 = package1.Workbook.Worksheets[0];
                var sheet2 = package2.Workbook.Worksheets[0];
                var outputSheet = outputPackage.Workbook.Worksheets.Add("CombinedSheet");

                int totalRows1 = sheet1.Dimension.Rows;
                int totalColumns1 = sheet1.Dimension.Columns;
                int totalRows2 = sheet2.Dimension.Rows;

                // Copia l'intestazione dal primo file Excel (riga 1)
                for (int col = 1; col <= totalColumns1; col++)
                {
                    outputSheet.Cells[1, col].Value = sheet1.Cells[1, col].Value;
                }

                // Unisci tutte le righe da entrambi i fogli (saltando l'intestazione)
                int currentRow = 2;
                for (int row = 2; row <= totalRows1; row++, currentRow++)
                {
                    for (int col = 1; col <= totalColumns1; col++)
                    {
                        outputSheet.Cells[currentRow, col].Value = sheet1.Cells[row, col].Value;
                    }
                }

                for (int row = 2; row <= totalRows2; row++, currentRow++)
                {
                    for (int col = 1; col <= totalColumns1; col++)
                    {
                        outputSheet.Cells[currentRow, col].Value = sheet2.Cells[row, col].Value;
                    }
                }

                // Ora mescola solo le righe (escludendo l'intestazione)
                var totalRowsCombined = currentRow - 1;
                var rows = Enumerable.Range(2, totalRowsCombined - 1).ToList();  // Prende tutte le righe tranne l'intestazione
                Random rand = new Random();
                rows = rows.OrderBy(x => rand.Next()).ToList();  // Mescola le righe

                // Crea un foglio temporaneo per tenere le righe mescolate
                var shuffledSheet = outputPackage.Workbook.Worksheets.Add("ShuffledSheet");

                // Copia l'intestazione nel foglio mescolato
                for (int col = 1; col <= totalColumns1; col++)
                {
                    shuffledSheet.Cells[1, col].Value = outputSheet.Cells[1, col].Value;
                }

                // Copia le righe mescolate nel nuovo foglio, mantenendo le celle intatte
                int newRow = 2;
                foreach (var row in rows)
                {
                    for (int col = 1; col <= totalColumns1; col++)
                    {
                        shuffledSheet.Cells[newRow, col].Value = outputSheet.Cells[row, col].Value;
                    }
                    newRow++;
                }

                // Rimuovi il vecchio foglio non mescolato
                outputPackage.Workbook.Worksheets.Delete("CombinedSheet");

                // Salva il file di output in un MemoryStream
                var stream = new MemoryStream();
                outputPackage.SaveAs(stream);
                stream.Position = 0;

                var fileName = "Combined_Utenze_telefoniche_Shuffled.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
    }
}
