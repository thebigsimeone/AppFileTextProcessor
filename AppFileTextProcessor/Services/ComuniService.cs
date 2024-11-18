namespace AppFileTextProcessor.Services
{
    using AppFileTextProcessor.Interface;
    using Newtonsoft.Json.Linq;

    public class ComuniService : IComuniService
    {
        private HashSet<string> _comuni;

        public ComuniService()
        {
            _comuni = new HashSet<string>();
        }

        public HashSet<string> LoadComuniFromJson(string path)
        {
            var json = System.IO.File.ReadAllText(path);
            var jsonArray = JArray.Parse(json);

            foreach (var item in jsonArray)
            {
                _comuni.Add(item["nome"].ToString().ToUpper());
            }

            return _comuni;
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
            }
            return string.Empty;
        }
    }

}
