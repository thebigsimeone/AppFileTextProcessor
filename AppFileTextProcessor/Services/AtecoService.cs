using AppFileTextProcessor.Interface;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace AppFileTextProcessor.Services
{
    public class AtecoService : IAtecoService
    {
        private JObject _atecoData;

        public AtecoService()
        {
            _atecoData = new JObject();
        }

        public void LoadAtecoFromJson(string atecoPath)
        {
            var jsonContent = File.ReadAllText(atecoPath);
            _atecoData = JObject.Parse(jsonContent);
        }

        public string GetAtecoDescription(string codiceAteco)
        {
            if (string.IsNullOrWhiteSpace(codiceAteco))
                return "N/A";

            // Pulizia input: rimuove spazi e caratteri non numerici
            string codicePulito = Regex.Replace(codiceAteco.Trim(), @"[^\d]", "");

            // Ricerca esatta e progressiva per livelli
            string? titoloEsatto = TrovaTitoloPerCodice(codicePulito);
            if (titoloEsatto != null)
            {
                return titoloEsatto;
            }

            // Tentativo di fallback: risaliamo ai primi 2 caratteri (categoria padre)
            if (codicePulito.Length >= 2)
            {
                string codicePadre = codicePulito.Substring(0, 2);
                string? titoloPadre = TrovaTitoloPerCodice(codicePadre);
                if (titoloPadre != null)
                {
                    return titoloPadre;
                }
            }

            return $"Codice attività non presente nella struttura ATECO: {codicePulito}";
        }

        private string? TrovaTitoloPerCodice(string codice)
        {
            if (string.IsNullOrEmpty(codice))
                return null;

            // Il primo carattere indica il nodo radice (0-9)
            string radice = codice[0].ToString();

            JToken? current = _atecoData[radice];

            if (current == null)
            {
                return null;
            }

            string ultimoTitoloValido = current["nome"]?.ToString() ?? "";

            // Segue la logica dei livelli ATECO: 2, 3, 4, 6 caratteri
            int[] livelli = { 2, 3, 4, 6 };

            foreach (int livello in livelli)
            {
                if (codice.Length < livello)
                    break;

                string prefisso = codice.Substring(0, livello);
                current = current["sottocategorie"]?[prefisso];

                if (current == null)
                    break;

                // Se troviamo un titolo valido, lo memorizziamo
                var nomeAttivita = current["nome"]?.ToString();
                if (!string.IsNullOrEmpty(nomeAttivita))
                {
                    ultimoTitoloValido = nomeAttivita;
                }
            }

            return string.IsNullOrEmpty(ultimoTitoloValido) ? null : ultimoTitoloValido;
        }
    }
}
