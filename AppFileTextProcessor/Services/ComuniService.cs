namespace AppFileTextProcessor.Services
{
    using AppFileTextProcessor.Interface;
    using Newtonsoft.Json.Linq;

    public class ComuniService : IComuniService
    {
        private HashSet<string> _comuni;
        private Dictionary<string, string> _elencoComuniItaliani;

        public ComuniService()
        {
            _comuni = new HashSet<string>();
            _elencoComuniItaliani = new Dictionary<string, string>();
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

            // Carica i comuni dal secondo file JSON (Elenco Comuni Italiani)
            var elencoJson = System.IO.File.ReadAllText(elencoComuniPath);
            var jsonObject = JObject.Parse(elencoJson);
            var elencoArray = (JArray)jsonObject["CODICI al 30-06-2024"];

            foreach (var item in elencoArray)
            {
                string nomeComune = item["Denominazione in italiano"].ToString().ToUpper();
                string codiceCatastale = item["Codice Catastale del comune"].ToString();
                _elencoComuniItaliani[nomeComune] = codiceCatastale;
            }
        }

        public string ExtractCityPart(string line)
        {
            var words = line.Split(' ');
            foreach (var word in words)
            {
                if (_comuni.Contains(word.ToUpper()))
                {
                    return word;
                }

                // Se il comune non è stato trovato, cerca anche nell'elenco dei comuni italiani
                if (_elencoComuniItaliani.ContainsKey(word.ToUpper()))
                {
                    return word;
                }
            }
            return string.Empty;
        }
    }
}
