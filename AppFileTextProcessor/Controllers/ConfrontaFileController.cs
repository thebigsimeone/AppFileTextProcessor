using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AppFileTextProcessor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfrontaFileController : ControllerBase
    {
        [HttpPost("confronta")]
        public IActionResult ConfrontaFile(IFormFile fileA, IFormFile fileB)
        {
            if (fileA == null || fileB == null || fileA.Length == 0 || fileB.Length == 0)
            {
                return BadRequest("Entrambi i file devono essere forniti e non vuoti.");
            }

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                // Caricamento del file A
                using var packageA = new ExcelPackage(fileA.OpenReadStream());
                var worksheetA = packageA.Workbook.Worksheets[0];
                int totalRowsA = worksheetA.Dimension.Rows;

                // Caricamento del file B
                using var packageB = new ExcelPackage(fileB.OpenReadStream());
                var worksheetB = packageB.Workbook.Worksheets[0];
                int totalRowsB = worksheetB.Dimension.Rows;

                // Creare un dizionario per i codici fiscali nel file B
                var datiFileB = new Dictionary<string, (string Stato, string EmailProfilo)>();

                // Popolare il dizionario con i dati dal file B
                for (int rowB = 2; rowB <= totalRowsB; rowB++)
                {
                    string codiceFiscaleB = worksheetB.Cells[rowB, 2].Text.Trim();
                    string stato = worksheetB.Cells[rowB, 4].Text.Trim();
                    string emailProfilo = worksheetB.Cells[rowB, 5].Text.Trim();

                    if (!string.IsNullOrEmpty(codiceFiscaleB))
                    {
                        datiFileB[codiceFiscaleB] = (stato, emailProfilo);
                    }
                }

                // Confronto con il file A e aggiornamento dei dati
                for (int rowA = 2; rowA <= totalRowsA; rowA++)
                {
                    string codiceFiscaleA = worksheetA.Cells[rowA, 2].Text.Trim();

                    if (datiFileB.ContainsKey(codiceFiscaleA))
                    {
                        var datiB = datiFileB[codiceFiscaleA];

                        // Aggiorna le colonne AW e AX nel file A (colonne 49 e 50)
                        worksheetA.Cells[rowA, 50].Value = datiB.Stato;       // Colonna AW
                        worksheetA.Cells[rowA, 51].Value = datiB.EmailProfilo; // Colonna AX
                    }
                }

                // Salvataggio del file aggiornato
                var stream = new MemoryStream();
                packageA.SaveAs(stream);
                stream.Position = 0;

                // Restituisci il file aggiornato
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "LISTA_NIS2_aggiornato.xlsx");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Errore interno: {ex.Message}");
            }
        }
    }
}
