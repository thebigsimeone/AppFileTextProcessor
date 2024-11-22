using AppFileTextProcessor.Interface;
using System.Text.RegularExpressions;
using System.Text;

namespace AppFileTextProcessor.Services
{
    public class TextProcessingService : ITextProcessingService
    {
        public List<(string Protocollo, string Identificativo, string Esito)> ProcessContent(string content)
        {
            var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var processedData = new List<(string Protocollo, string Identificativo, string Esito)>();

            string protocollo = null;
            string identificativo = null;
            StringBuilder esito = new StringBuilder();
            bool newRecord = false;

            foreach (var line in lines)
            {
                string cleanedLine = Regex.Replace(line, "[\t\x00-\x1F]+", " ").Trim();
                cleanedLine = Regex.Replace(cleanedLine, "\\s+", " "); // Rimuove spazi multipli e tabulazioni
                cleanedLine = NormalizeText(cleanedLine); // Normalizza il testo

                // Riconoscere la riga contenente il Protocollo
                if (Regex.IsMatch(cleanedLine, @"^2024\d{7}\s[A-Z0-9]{11,16}$"))
                {
                    if (newRecord)
                    {
                        processedData.Add((protocollo, identificativo, esito.ToString().Trim()));
                        esito.Clear();
                    }

                    protocollo = cleanedLine.Substring(0, 11);
                    identificativo = cleanedLine.Substring(12);
                    newRecord = true;
                }
                else
                {
                    if (newRecord)
                    {
                        esito.AppendLine(cleanedLine);
                    }
                }
            }

            if (newRecord)
            {
                processedData.Add((protocollo, identificativo, esito.ToString().Trim()));
            }

            return processedData;
        }

        private string NormalizeText(string input)
        {
            // Normalizza il testo rimuovendo caratteri speciali e invisibili
            input = Regex.Replace(input, "[\u200B-\u200D\uFEFF]", ""); // Rimuove caratteri zero-width
            input = Regex.Replace(input, "[\x00-\x1F\x7F]+", " "); // Rimuove caratteri di controllo non stampabili
            input = Regex.Replace(input, "\\s+", " ").Trim(); // Rimuove spazi multipli e trim
            return input;
        }
    }
}
