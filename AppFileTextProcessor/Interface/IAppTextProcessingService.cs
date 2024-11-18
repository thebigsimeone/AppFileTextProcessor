namespace AppFileTextProcessor.Interface
{
    public interface IAppTextProcessingService
    {
        Task<string> ProcessContentAsync(string content);
    }

}
