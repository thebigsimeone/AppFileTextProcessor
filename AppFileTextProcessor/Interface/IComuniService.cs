namespace AppFileTextProcessor.Interface
{
    public interface IComuniService
    {
        HashSet<string> LoadComuniFromJson(string path);
        string ExtractCityPart(string line);
    }

}
