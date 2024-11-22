namespace AppFileTextProcessor.Services
{
    using AppFileTextProcessor.Interface;
    using Newtonsoft.Json.Linq;
    using System.Text.RegularExpressions;

    public class ComuniService : IComuniService
    {
        private HashSet<string> _comuni;
        private HashSet<string> _elencoComuniItaliani;

        public ComuniService()
        {
            _comuni = new HashSet<string>();
            _elencoComuniItaliani = new HashSet<string>();
        }

        public void LoadComuniFromJson(string comuniPath, string elencoComuniPath)
        {
            // Carica i comuni dal primo file JSON
            var comuniJson = System.IO.File.ReadAllText(comuniPath);
            var comuniArray = JArray.Parse(comuniJson);

            foreach (var item in comuniArray)
            {
                _comuni.Add(item["nome"].ToString().ToUpper());
            }

            // Carica le denominazioni dei comuni dal secondo file JSON (Elenco Comuni Italiani)
            var elencoJson = System.IO.File.ReadAllText(elencoComuniPath);
            var jsonObject = JObject.Parse(elencoJson);
            var elencoArray = (JArray)jsonObject["CODICI al 30-06-2024"];

            foreach (var item in elencoArray)
            {
                string nomeComune = item["Denominazione in italiano"].ToString().ToUpper();
                _elencoComuniItaliani.Add(nomeComune);
            }
        }

        public string ExtractCityPart(string line)
        {
            var cleanedLine = Regex.Replace(line.Trim(), @"\s+", " ").ToUpper();

            var words = cleanedLine.Split(' ');

            foreach (var word in words)
            {
                // Cerca nei comuni dal primo file JSON
                if (_comuni.Contains(word))
                {
                    return word;
                }

                if (_elencoComuniItaliani.Contains(word))
                {
                    return word;
                }
            }
            return string.Empty;
        }
    }
}
