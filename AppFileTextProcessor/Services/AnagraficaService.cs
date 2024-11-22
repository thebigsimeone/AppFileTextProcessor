using AppFileTextProcessor.Interface;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace AppFileTextProcessor.Services
{
    public class AnagraficaService : IAnagraficaService
    {
        private readonly IConfiguration _configuration;

        public AnagraficaService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> TrovaDenominazioneAsync(string codiceFiscale, string partitaIva)
        {
            string connectionStringEbi = _configuration.GetConnectionString("DefaultConnection_EBI");
            string connectionStringSsc = _configuration.GetConnectionString("DefaultConnection_SSC");

            // Normalizza codice fiscale e partita IVA
            codiceFiscale = codiceFiscale?.Trim().ToUpper();
            partitaIva = partitaIva?.Trim().ToUpper();

            // La query per cercare la denominazione usando codice fiscale o partita iva
            string query = @"
                SELECT DISTINCT PBACFI, PBAPIV, PBADEN 
                FROM pbardgf0 
                WHERE PBAANP BETWEEN 2014 AND 2024
                    AND (@CodiceFiscale IS NULL OR PBACFI = @CodiceFiscale)
                    AND (@PartitaIva IS NULL OR PBAPIV = @PartitaIva)";

            // Primo tentativo con il database EBI
            string denominazione = await TrovaDenominazioneNelDatabaseAsync(connectionStringEbi, query, codiceFiscale, partitaIva);

            if (!string.IsNullOrEmpty(denominazione))
            {
                return denominazione;
            }

            // Se non è stato trovato nel database EBI, prova con il database SSC
            return await TrovaDenominazioneNelDatabaseAsync(connectionStringSsc, query, codiceFiscale, partitaIva);
        }

        private async Task<string> TrovaDenominazioneNelDatabaseAsync(string connectionString, string query, string codiceFiscale, string partitaIva)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CodiceFiscale", string.IsNullOrWhiteSpace(codiceFiscale) ? DBNull.Value : (object)codiceFiscale);
                    command.Parameters.AddWithValue("@PartitaIva", string.IsNullOrWhiteSpace(partitaIva) ? DBNull.Value : (object)partitaIva);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // Restituisci la denominazione eliminando eventuali spazi aggiuntivi
                            return reader["PBADEN"].ToString().Trim().ToUpper();
                        }
                    }
                }
            }
            return null;
        }
    }
}
