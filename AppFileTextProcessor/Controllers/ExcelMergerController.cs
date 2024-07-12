using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System;
using System.IO;
using System.Threading.Tasks;

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

                // Combine the rows from both sheets
                for (int row = 1; row <= totalRows1; row++)
                {
                    for (int col = 1; col <= totalColumns1; col++)
                    {
                        outputSheet.Cells[row, col].Value = sheet1.Cells[row, col].Value;
                    }
                }

                for (int row = 1; row <= totalRows2; row++)
                {
                    for (int col = 1; col <= totalColumns1; col++)
                    {
                        outputSheet.Cells[totalRows1 + row, col].Value = sheet2.Cells[row, col].Value;
                    }
                }

                int totalRowsCombined = totalRows1 + totalRows2;

                // Shuffle the combined rows, excluding the header
                Random rand = new Random();
                var rows = Enumerable.Range(2, totalRowsCombined - 1).OrderBy(x => rand.Next()).ToList();
                for (int i = 0; i < rows.Count; i++)
                {
                    for (int col = 1; col <= totalColumns1; col++)
                    {
                        outputSheet.Cells[i + 2, col].Value = outputSheet.Cells[rows[i], col].Value;
                    }
                }

                // Save the output file to a memory stream
                var stream = new MemoryStream();
                outputPackage.SaveAs(stream);
                stream.Position = 0;

                var fileName = "Combined_Utenze_telefoniche.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
    }
}
