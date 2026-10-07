using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;
using System.Drawing;

namespace AppFileTextProcessor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MergerExcelFilesController : ControllerBase
    {
        /// <summary>
        /// Unisce due file Excel e genera un nuovo file con i dati combinati.
        /// </summary>
        /// <param name="fileTENANT_B">Primo file Excel</param>
        /// <param name="fileTENANT_A">Secondo file Excel</param>
        /// <returns>File Excel unito</returns>
        [HttpPost("merge")]
        [SwaggerOperation(Summary = "Unisce due file Excel", Description = "Combina due file Excel mantenendo l'intestazione e unendo i dati.")]
        [SwaggerResponse(200, "File elaborato e restituito con successo.")]
        [SwaggerResponse(400, "Entrambi i file sono richiesti.")]
        [SwaggerResponse(500, "Errore interno del server.")]
        public async Task<IActionResult> MergeExcelFiles(IFormFile fileTENANT_B, IFormFile fileTENANT_A)
        {
            if (fileTENANT_B == null || fileTENANT_A == null)
            {
                Log.Warning("Uno o entrambi i file non sono stati forniti.");
                return BadRequest("Entrambi i file sono richiesti.");
            }

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var packageTENANT_B = new ExcelPackage(fileTENANT_B.OpenReadStream()))
                using (var packageTENANT_A = new ExcelPackage(fileTENANT_A.OpenReadStream()))
                using (var outputPackage = new ExcelPackage())
                {
                    var sheetTENANT_B = packageTENANT_B.Workbook.Worksheets[0];
                    var sheetTENANT_A = packageTENANT_A.Workbook.Worksheets[0];
                    var outputSheet = outputPackage.Workbook.Worksheets.Add("MergedSheet");

                    int totalColumns = sheetTENANT_B.Dimension.Columns;
                    Log.Information("Fusione di due file Excel: {File1} ({Rows1} righe), {File2} ({Rows2} righe)", fileTENANT_B.FileName, sheetTENANT_B.Dimension.Rows, fileTENANT_A.FileName, sheetTENANT_A.Dimension.Rows);

                    CopyHeader(sheetTENANT_B, outputSheet, totalColumns);

                    int currentRow = CopyRows(sheetTENANT_B, outputSheet, 2, totalColumns);
                    currentRow = CopyRows(sheetTENANT_A, outputSheet, currentRow, totalColumns);

                    int totalRows = currentRow - 2;
                    Log.Information("Unione completata: {TotalRows} righe unite.", totalRows);

                    // Adatta automaticamente la larghezza delle colonne
                    outputSheet.Cells[outputSheet.Dimension.Address].AutoFitColumns();

                    string outputFileName = GenerateOutputFileName(fileTENANT_B.FileName, fileTENANT_A.FileName, totalRows);

                    var stream = new MemoryStream();
                    outputPackage.SaveAs(stream);
                    stream.Position = 0;

                    Log.Information("File elaborato e salvato: {FileName}", outputFileName);
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", outputFileName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Errore durante la fusione dei file Excel.");
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }

        private void CopyHeader(ExcelWorksheet sourceSheet, ExcelWorksheet targetSheet, int totalColumns)
        {
            for (int col = 1; col <= totalColumns; col++)
            {
                var cell = targetSheet.Cells[1, col];
                cell.Value = sourceSheet.Cells[1, col].Value;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                cell.Style.Font.Color.SetColor(Color.Black);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                targetSheet.Column(col).Style.Numberformat.Format = "@";
            }
        }

        private int CopyRows(ExcelWorksheet sourceSheet, ExcelWorksheet targetSheet, int startRow, int totalColumns)
        {
            int currentRow = startRow;

            for (int row = 2; row <= sourceSheet.Dimension.Rows; row++, currentRow++)
            {
                for (int col = 1; col <= totalColumns; col++)
                {
                    targetSheet.Cells[currentRow, col].Value = sourceSheet.Cells[row, col].Value;
                }
            }

            return currentRow;
        }

        private string GenerateOutputFileName(string fileTENANT_BName, string fileTENANT_AName, int totalRows)
        {
            string baseName = Path.GetFileNameWithoutExtension(fileTENANT_BName);
            return $"{baseName}_{totalRows}.xlsx";
        }
    }
}

