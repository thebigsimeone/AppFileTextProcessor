namespace AppFileTextProcessor.Interface
{
    public interface IAnagraficaService
    {
        Task<string> TrovaDenominazioneAsync(string codiceFiscale, string partitaIva);
    }
}
