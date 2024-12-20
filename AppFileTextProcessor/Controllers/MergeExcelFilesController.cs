using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace AppFileTextProcessor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MergerExcelFilesController : ControllerBase
    {
        [HttpPost("merge")]
        public async Task<IActionResult> MergeExcelFiles(IFormFile fileSSC, IFormFile fileEBI)
        {
            if (fileSSC == null || fileEBI == null)
            {
                return BadRequest("Entrambi i file sono richiesti.");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            try
            {
                using (var packageSSC = new ExcelPackage(fileSSC.OpenReadStream()))
                using (var packageEBI = new ExcelPackage(fileEBI.OpenReadStream()))
                using (var outputPackage = new ExcelPackage())
                {
                    var sheetSSC = packageSSC.Workbook.Worksheets[0];
                    var sheetEBI = packageEBI.Workbook.Worksheets[0];
                    var outputSheet = outputPackage.Workbook.Worksheets.Add("MergedSheet");

                    int totalColumns = sheetSSC.Dimension.Columns;

                    CopyHeader(sheetSSC, outputSheet, totalColumns);

                    int currentRow = CopyRows(sheetSSC, outputSheet, 2, totalColumns);
                    currentRow = CopyRows(sheetEBI, outputSheet, currentRow, totalColumns);

                    int totalRows = currentRow - 2;

                    // Adatta automaticamente la larghezza delle colonne
                    outputSheet.Cells[outputSheet.Dimension.Address].AutoFitColumns();

                    string outputFileName = GenerateOutputFileName(fileSSC.FileName, fileEBI.FileName, totalRows);

                    var stream = new MemoryStream();
                    outputPackage.SaveAs(stream);
                    stream.Position = 0;

                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", outputFileName);
                }
            }
            catch (Exception ex)
            {
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

        private string GenerateOutputFileName(string fileSSCName, string fileEBIName, int totalRows)
        {
            string baseName = Path.GetFileNameWithoutExtension(fileSSCName);
            return $"{baseName}_{totalRows}.xlsx";
        }
    }
}
