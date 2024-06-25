using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TextFileProcessor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppTextConvertController : ControllerBase
    {
        private readonly HashSet<string> _comuni;

        public AppTextConvertController()
        {
            // Carica i comuni dal file JSON
            _comuni = LoadComuniFromJson(@"C:\Users\Utente\Desktop\PublishedApp\AppFileTextProcessor\json\comuni.json");
        }

        private const string BaseDirectory = @"C:\Users\Utente\Desktop\APPALTO\";

        [HttpPost("process")]
        public IActionResult ProcessLocalFile([FromQuery] string inputFileName, [FromQuery] string outputFileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(inputFileName) || string.IsNullOrWhiteSpace(outputFileName))
                {
                    return BadRequest("I nomi dei file di input e output sono obbligatori.");
                }

                string inputFilePath = Path.Combine(BaseDirectory, inputFileName + ".txt");
                string outputFilePath = Path.Combine(BaseDirectory, outputFileName + ".txt");

                if (!System.IO.File.Exists(inputFilePath))
                {
                    return BadRequest("File di input non trovato.");
                }

                Console.WriteLine($"Input File Path: {inputFilePath}");
                Console.WriteLine($"Output File Path: {outputFilePath}");

                string content;
                using (var reader = new StreamReader(inputFilePath, Encoding.UTF8))
                {
                    content = reader.ReadToEnd();
                }

                string processedContent = ProcessContent(content);
                System.IO.File.WriteAllText(outputFilePath, processedContent);

                return Ok("File elaborato e salvato correttamente.");
            }
            catch (Exception ex)
            {
                // Log l'errore per diagnosi
                Console.WriteLine($"Errore: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }

        private string ProcessContent(string content)
        {
            var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var processedLines = new List<string>();

            // Aggiungi l'intestazione iniziale
            processedLines.Add("Dai controlli effettuati in capo al relazionato sono stati rilevati i seguenti negozi");
            processedLines.Add("giuridici:");
            processedLines.Add("-");
            processedLines.Add("-");

            bool newSection = true;

            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();
                var partitaiva = @"^\d{11}$";
                var codicefiscale = @"^[A-Z]{6}\d{2}[A-Z]\d{2}[A-Z]\d{3}[A-Z]$";

                // Riconoscere e processare la riga contenente l'anno
                if (Regex.IsMatch(trimmedLine, @"^\d{4}(\s+Modello\s+.+)?$"))
                {
                    // Gestisci l'inizio di un nuovo anno
                    if (!newSection)
                    {
                        processedLines.Add("-");
                        processedLines.Add("-");
                    }
                    processedLines.Add("ANNO " + trimmedLine.Split('\t')[0]);
                    processedLines.Add("-");
                    processedLines.Add("-");
                    newSection = true;
                }
                else if (trimmedLine.StartsWith("Modello") || trimmedLine.StartsWith("Serie") || trimmedLine.StartsWith("Codice identificativo contratto") || trimmedLine.StartsWith("Protocollo Telematico"))
                {
                    // Rimuovi le righe "Modello", "Serie", "Codice identificativo contratto" e "Protocollo Telematico"
                    continue;
                }
                else if (trimmedLine.StartsWith("Ufficio"))
                {
                    // Processa la riga "Ufficio"
                    string cityPart = ExtractCityPart(trimmedLine);
                    string datePart = trimmedLine.Split(new[] { "data registrazione" }, StringSplitOptions.None)[1].Trim();
                    processedLines.Add("--Ufficio " + cityPart + " data registrazione " + datePart);
                }
                else if (trimmedLine.StartsWith("Negozio"))
                {
                    // Processa la riga "Negozio"
                    trimmedLine = Regex.Replace(trimmedLine, @"\s*\([^)]*\)", string.Empty);
                    processedLines.Add(trimmedLine);
                }
                else if (Regex.IsMatch(trimmedLine, partitaiva))
                {
                    processedLines.Add($"({trimmedLine})");
                }
                else if (Regex.IsMatch(trimmedLine, codicefiscale))
                {
                    processedLines.Add($"({trimmedLine})");
                }
                else
                {
                    processedLines.Add(trimmedLine);
                }

                newSection = false;
            }

            // Rimuovi eventuali trattini superflui e linee vuote
            for (int i = processedLines.Count - 1; i > 0; i--)
            {
                if (string.IsNullOrWhiteSpace(processedLines[i]))
                {
                    processedLines.RemoveAt(i);
                }
            }

            processedLines.Add("-");
            processedLines.Add("-");
            // Aggiungi la riga finale
            processedLines.Add("Dai controlli effettuati non sono stati rilevati ulteriori negozi giuridici");

            // Unisci di nuovo le linee processate
            content = string.Join("\n", processedLines);

            return content;
        }

        private string ExtractCityPart(string line)
        {
            var words = line.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (_comuni.Contains(words[i]))
                {
                    return words[i];
                }
            }
            return string.Empty;
        }

        private HashSet<string> LoadComuniFromJson(string path)
        {
            var comuni = new HashSet<string>();
            var json = System.IO.File.ReadAllText(path);
            var jsonArray = JArray.Parse(json);

            foreach (var item in jsonArray)
            {
                comuni.Add(item["nome"].ToString().ToUpper());
            }

            return comuni;
        }
    }
}
