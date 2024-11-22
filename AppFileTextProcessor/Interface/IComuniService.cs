namespace AppFileTextProcessor.Interface
{
    public interface IComuniService
    {
        void LoadComuniFromJson(string comuniPath, string elencoComuniPath);
        string ExtractCityPart(string line);
    }
}
