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
            string connectionStringTenantA = _configuration.GetConnectionString("DefaultConnection_TENANT_A");
            string connectionStringTenantB = _configuration.GetConnectionString("DefaultConnection_TENANT_B");

            // Normalizza codice fiscale e partita IVA
            codiceFiscale = codiceFiscale?.Trim().ToUpper();
            partitaIva = partitaIva?.Trim().ToUpper();

            // La query per cercare la denominazione usando codice fiscale o partita iva
            string query = @"
                SELECT DISTINCT CodiceFiscale, PartitaIva, Denominazione 
                FROM Pratiche 
                WHERE AnnoProtocollo BETWEEN 2014 AND 2024
                    AND (@CodiceFiscale IS NULL OR CodiceFiscale = @CodiceFiscale)
                    AND (@PartitaIva IS NULL OR PartitaIva = @PartitaIva)";

            // Primo tentativo con il database TENANT_A
            string denominazione = await TrovaDenominazioneNelDatabaseAsync(connectionStringTenantA, query, codiceFiscale, partitaIva);

            if (!string.IsNullOrEmpty(denominazione))
            {
                return denominazione;
            }

            // Se non è stato trovato nel database TENANT_A, prova con il database TENANT_B
            return await TrovaDenominazioneNelDatabaseAsync(connectionStringTenantB, query, codiceFiscale, partitaIva);
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
                            return reader["Denominazione"].ToString().Trim().ToUpper();
                        }
                    }
                }
            }
            return null;
        }
    }
}

