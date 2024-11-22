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
                // Pulisce la linea dai caratteri invisibili e non stampabili, escludendo i caratteri con accenti
                string cleanedLine = RimuoviCaratteriInvisibili(line);

                string trimmedLine = cleanedLine.Trim();

                // Riconoscere la riga contenente il Protocollo
                if (Regex.IsMatch(trimmedLine, @"^2024\d{7}\s[A-Z0-9]{11,16}$"))
                {
                    if (newRecord)
                    {
                        processedData.Add((protocollo, identificativo, esito.ToString().Trim()));
                        esito.Clear();
                    }

                    protocollo = trimmedLine.Substring(0, 11);
                    identificativo = trimmedLine.Substring(12);
                    newRecord = true;
                }
                else
                {
                    if (newRecord)
                    {
                        esito.AppendLine(trimmedLine);
                    }
                }
            }

            if (newRecord)
            {
                processedData.Add((protocollo, identificativo, esito.ToString().Trim()));
            }

            return processedData;
        }

        // Funzione per rimuovere i caratteri invisibili e non stampabili
        private string RimuoviCaratteriInvisibili(string input)
        {
            // Rimuove caratteri che non sono visibili, tranne gli accenti e i caratteri standard visibili
            return Regex.Replace(input, @"[^\x20-\x7EÀ-ÿ]", " ");
        }
    }
}
