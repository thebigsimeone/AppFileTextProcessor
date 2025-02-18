namespace AppFileTextProcessor.Interface
{
    public interface IAtecoService
    {
        void LoadAtecoFromJson(string atecoPath);
        string GetAtecoDescription(string codiceAteco);
    }
}
