using AppFileTextProcessor.Interface;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class AppTextProcessingService : IAppTextProcessingService
{
    private readonly IComuniService _comuniService;
    private readonly IAnagraficaService _anagraficaService;

    public AppTextProcessingService(IComuniService comuniService, IAnagraficaService anagraficaService)
    {
        _comuniService = comuniService;
        _anagraficaService = anagraficaService;
    }

    public async Task<string> ProcessContentAsync(string content)
    {
        var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var processedLines = new List<string>();

        // Rimuovi eventuali caratteri speciali non visibili
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = Regex.Replace(lines[i], @"[^\x20-\x7E]", " ");
        }

        processedLines.Add("Dai controlli effettuati in capo al relazionato sono stati rilevati i seguenti negozi");
        processedLines.Add("giuridici:");
        processedLines.Add("-");
        processedLines.Add("-");

        bool newSection = true;

        foreach (var line in lines)
        {
            string trimmedLine = line.Trim();
            trimmedLine = Regex.Replace(trimmedLine, @"\s+", " "); // Rimuove spazi multipli
            var partitaivaPattern = @"^\d{11}$";
            var codicefiscalePattern = @"^[A-Z]{6}\d{2}[A-Z]\d{2}[A-Z]\d{3}[A-Z]$";

            // Riconoscere e processare la riga contenente l'anno
            if (Regex.IsMatch(trimmedLine, @"^\d{4}\b"))
            {
                if (!newSection)
                {
                    processedLines.Add("-");
                    processedLines.Add("-");
                }

                string anno = trimmedLine.Split('\t')[0];
                processedLines.Add($"ANNO {anno}");
                processedLines.Add("-");

                newSection = true;
                continue;
            }
            else if (Regex.IsMatch(trimmedLine, @"^Modello|Serie|Codice identificativo contratto|Protocollo Telematico"))
            {
                continue; // Salta righe inutili
            }
            else if (trimmedLine.StartsWith("Ufficio"))
            {
                string cityPart = _comuniService.ExtractCityPart(trimmedLine);
                string datePart = trimmedLine.Split(new[] { "data registrazione" }, StringSplitOptions.None)[1].Trim();
                processedLines.Add("-");
                processedLines.Add("--Ufficio " + cityPart + " data registrazione " + datePart);
            }
            else if (trimmedLine.StartsWith("Negozio"))
            {
                trimmedLine = Regex.Replace(trimmedLine, @"\s*\([^)]*\)", string.Empty);
                processedLines.Add(trimmedLine);
            }
            else if (Regex.IsMatch(trimmedLine, partitaivaPattern) || Regex.IsMatch(trimmedLine, codicefiscalePattern))
            {
                string codiceFiscale = Regex.IsMatch(trimmedLine, codicefiscalePattern) ? trimmedLine : null;
                string partitaIva = Regex.IsMatch(trimmedLine, partitaivaPattern) ? trimmedLine : null;

                string denominazione = await _anagraficaService.TrovaDenominazioneAsync(codiceFiscale, partitaIva);

                if (!string.IsNullOrEmpty(denominazione))
                {
                    processedLines.Add($"{denominazione} ({trimmedLine})");
                }
                else
                {
                    processedLines.Add($"({trimmedLine})");
                }
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
        processedLines.Add("Dai controlli effettuati non sono stati rilevati ulteriori negozi giuridici");

        return string.Join("\n", processedLines);
    }
}
