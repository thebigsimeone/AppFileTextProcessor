using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;

namespace AppFileTextProcessor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfrontaFileController : ControllerBase
    {
        /// <summary>
        /// Confronta due file Excel e aggiorna i dati in base al confronto.
        /// </summary>
        /// <param name="fileA">Primo file Excel (base di confronto)</param>
        /// <param name="fileB">Secondo file Excel (dati aggiornati)</param>
        /// <returns>File Excel aggiornato</returns>
        [HttpPost("confronta")]
        [SwaggerOperation(Summary = "Confronta due file Excel", Description = "Confronta i codici fiscali tra due file Excel e aggiorna i dati.")]
        [SwaggerResponse(200, "File elaborato e restituito con successo.")]
        [SwaggerResponse(400, "Entrambi i file devono essere forniti e non vuoti.")]
        [SwaggerResponse(500, "Errore interno del server.")]
        public IActionResult ConfrontaFile(IFormFile fileA, IFormFile fileB)
        {
            if (fileA == null || fileB == null || fileA.Length == 0 || fileB.Length == 0)
            {
                Log.Warning("File non validi: fileA = {FileA}, fileB = {FileB}", fileA?.FileName, fileB?.FileName);
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

                Log.Information("File A: {FileA}, righe: {TotalRowsA}", fileA.FileName, totalRowsA);
                Log.Information("File B: {FileB}, righe: {TotalRowsB}", fileB.FileName, totalRowsB);

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

                Log.Information("Dizionario creato con {Count} codici fiscali da file B.", datiFileB.Count);

                // Confronto con il file A e aggiornamento dei dati
                int aggiornamenti = 0;
                for (int rowA = 2; rowA <= totalRowsA; rowA++)
                {
                    string codiceFiscaleA = worksheetA.Cells[rowA, 2].Text.Trim();

                    if (datiFileB.ContainsKey(codiceFiscaleA))
                    {
                        var datiB = datiFileB[codiceFiscaleA];

                        // Aggiorna le colonne AW e AX nel file A (colonne 50 e 51)
                        worksheetA.Cells[rowA, 50].Value = datiB.Stato;       // Colonna AW
                        worksheetA.Cells[rowA, 51].Value = datiB.EmailProfilo; // Colonna AX
                        aggiornamenti++;
                    }
                }

                Log.Information("Aggiornati {Count} record nel file A.", aggiornamenti);

                // Salvataggio del file aggiornato
                var stream = new MemoryStream();
                packageA.SaveAs(stream);
                stream.Position = 0;

                Log.Information("File elaborato con successo, pronto per il download.");
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "LISTA_NIS2_aggiornato.xlsx");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Errore durante il confronto dei file Excel.");
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }
    }
}
