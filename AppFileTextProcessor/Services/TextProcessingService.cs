using AppFileTextProcessor.Interface;
using System.Text.RegularExpressions;
using System.Text;

namespace AppFileTextProcessor.Service
{
    public class TextProcessingService : ITextProcessingService
    {
        public List<(string Protocollo, string Identificativo, string Esito)> ProcessContent(string content)
        {
            var data = new List<(string Protocollo, string Identificativo, string Esito)>();
            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            string protocolloPattern = "^2024\\d{7}";
            string identificativoPattern = "([A-Z0-9]{16}|\\d{11})";

            string currentProtocollo = null;
            string currentIdentificativo = null;
            StringBuilder esitoBuilder = new StringBuilder();

            foreach (var line in lines)
            {
                string trimmedLine = line.TrimEnd();

                if (Regex.IsMatch(trimmedLine, protocolloPattern) && Regex.IsMatch(trimmedLine, identificativoPattern))
                {
                    if (currentProtocollo != null && currentIdentificativo != null)
                    {
                        string esito = esitoBuilder.ToString().Trim();
                        data.Add((currentProtocollo, currentIdentificativo, esito));
                    }
                    esitoBuilder.Clear();

                    var parts = trimmedLine.Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        currentProtocollo = parts[0].Trim();
                        currentIdentificativo = parts[1].Trim();
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(trimmedLine))
                    {
                        esitoBuilder.AppendLine(trimmedLine);
                    }
                }
            }

            if (currentProtocollo != null && currentIdentificativo != null)
            {
                string esito = esitoBuilder.ToString().Trim();
                data.Add((currentProtocollo, currentIdentificativo, esito));
            }

            return data;
        }
    }
}
