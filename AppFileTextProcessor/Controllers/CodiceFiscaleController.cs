using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AppFileTextProcessor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CodiceFiscaleController : ControllerBase
    {
        private readonly Dictionary<string, string> _comuni;
        private readonly Dictionary<string, string> _elencoComuniItaliani;
        private readonly Dictionary<string, string> _elencoStatiEsteri;

        public CodiceFiscaleController()
        {
            _comuni = LoadComuniFromJson(@"C:\Users\Utente\Desktop\PublishedApp\AppFileTextProcessor\json\comuni.json");
            _elencoComuniItaliani = LoadElencoComuniItalianiFromJson(@"C:\Users\Utente\Desktop\PublishedApp\AppFileTextProcessor\json\Elenco-comuni-italiani.json");
            _elencoStatiEsteri = LoadElencoStatiEsteriFromJson(@"C:\Users\Utente\Desktop\PublishedApp\AppFileTextProcessor\json\Elenco-stati-esteri.json");
        }

        [HttpPost("process")]
        public IActionResult ProcessExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File non trovato o vuoto.");
            }

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(file.OpenReadStream()))
                {
                    var worksheet = package.Workbook.Worksheets[0];

                    int totalRows = worksheet.Dimension.Rows;

                    for (int row = 2; row <= totalRows; row++)  // Salta l'intestazione
                    {
                        string cognome = worksheet.Cells[row, 1].Text;
                        string nome = worksheet.Cells[row, 2].Text;
                        string sesso = worksheet.Cells[row, 3].Text;
                        string dataNascitaStr = worksheet.Cells[row, 4].Text;
                        string luogoNascita = worksheet.Cells[row, 5].Text;
                        string provincia = worksheet.Cells[row, 6].Text;

                        DateTime dataNascita = DateTime.ParseExact(dataNascitaStr, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                        string codiceFiscale = CalcolaCodiceFiscale(cognome, nome, sesso, dataNascita, luogoNascita, provincia);
                        if (codiceFiscale.Contains("Comune non trovato"))
                        {
                            worksheet.Cells[row, 7].Value = "Comune non trovato: " + luogoNascita;
                        }
                        else
                        {
                            worksheet.Cells[row, 7].Value = codiceFiscale;  // Inserisce il codice fiscale nella settima colonna
                        }
                        
                    }

                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    // Ritorna il file elaborato
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Codici_Fiscali_Elaborati.xlsx");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Errore interno: {ex.Message}");
            }
        }

        private string CalcolaCodiceFiscale(string cognome, string nome, string sesso, DateTime dataNascita, string luogoNascita, string provincia)
        {
            string codiceCognome = CalcolaCognome(cognome);
            string codiceNome = CalcolaNome(nome);
            string codiceDataSesso = CalcolaDataSesso(dataNascita, sesso);
            string codiceCatastale = CalcolaCodiceCatastale(luogoNascita, provincia);
            string codiceBase = codiceCognome + codiceNome + codiceDataSesso + codiceCatastale;
            string carattereControllo = CalcolaCarattereControllo(codiceBase);

            return codiceBase + carattereControllo;
        }

        private string CalcolaCognome(string cognome)
        {
            // Rimuove spazi e converte tutto in maiuscolo
            cognome = RimuoviSpaziNonNecessari(cognome);

            // Raccoglie tutte le consonanti
            string consonanti = string.Concat(cognome.Where(c => !"AEIOU".Contains(char.ToUpper(c))));
            string vocali = string.Concat(cognome.Where(c => "AEIOU".Contains(char.ToUpper(c))));

            // Se il cognome ha almeno 3 consonanti, prendi le prime 3 consonanti
            if (consonanti.Length >= 3)
            {
                return consonanti.Substring(0, 3).ToUpper();
            }
            // Se ci sono meno di 3 consonanti, aggiungi vocali fino a raggiungere 3 caratteri
            else if (consonanti.Length + vocali.Length >= 3)
            {
                return (consonanti + vocali).Substring(0, 3).ToUpper();
            }
            // Se ci sono meno di 3 caratteri in totale, aggiungi 'X' fino a raggiungere 3 caratteri
            else
            {
                return (consonanti + vocali + "X").PadRight(3, 'X').ToUpper();
            }
        }
        private string CalcolaNome(string nome)
        {
            // Rimuove gli spazi e converte tutto in maiuscolo
            nome = RimuoviSpaziNonNecessari(nome);

            // Raccoglie tutte le consonanti
            string consonanti = string.Concat(nome.Where(c => !"AEIOU".Contains(char.ToUpper(c))));
            // Raccoglie tutte le vocali
            string vocali = string.Concat(nome.Where(c => "AEIOU".Contains(char.ToUpper(c))));

            // Regola 1: Se ci sono 4 o più consonanti, prendi la 1ª, 3ª e 4ª consonante
            if (consonanti.Length >= 4)
            {
                return (consonanti[0].ToString() + consonanti[2].ToString() + consonanti[3].ToString()).ToUpper();
            }
            // Regola 2: Se ci sono esattamente 3 consonanti, prendi le prime 3 consonanti
            else if (consonanti.Length == 3)
            {
                return consonanti.ToUpper();
            }
            // Regola 3: Se ci sono meno di 3 consonanti, aggiungi vocali fino ad arrivare a 3 caratteri
            else if (consonanti.Length + vocali.Length >= 3)
            {
                return (consonanti + vocali).Substring(0, 3).ToUpper();
            }
            // Regola 4: Se ci sono meno di 3 caratteri in totale, aggiungi 'X' fino a raggiungere 3 caratteri
            else
            {
                return (consonanti + vocali + "X").PadRight(3, 'X').ToUpper();
            }
        }
        private string CalcolaDataSesso(DateTime dataNascita, string sesso)
        {
            string anno = dataNascita.ToString("yy");
            string mese = "ABCDEHLMPRST"[dataNascita.Month - 1].ToString();
            string giorno = (sesso.ToUpper() == "M" ? dataNascita.Day : dataNascita.Day + 40).ToString("D2");

            return anno + mese + giorno;
        }

        private string CalcolaCodiceCatastale(string luogoNascita, string provincia)
        {
            // Normalizza il nome del comune rimuovendo spazi e caratteri speciali
            luogoNascita = NormalizeComuneName(luogoNascita);

            if (_comuni.ContainsKey(luogoNascita))
            {
                return _comuni[luogoNascita];
            }
            else if (_elencoComuniItaliani.ContainsKey(luogoNascita))  // Cerca nel secondo file JSON
            {
                return _elencoComuniItaliani[luogoNascita];
            }
            else if (_elencoStatiEsteri.ContainsKey(luogoNascita))  // Cerca nel secondo file JSON
            {
                return _elencoStatiEsteri[luogoNascita];
            }
            else
            {
                return "Comune non trovato";  // Restituisci un messaggio nel caso non trovi il comune
            }
        }

        private string NormalizeComuneName(string name)
        {
            // Rimuove spazi e normalizza maiuscole/minuscole
            return Regex.Replace(name.Trim().ToUpper(), @"\s+", " ");
        }

        private string RimuoviSpaziNonNecessari(string input)
        {
            // Rimuove tutti gli spazi nel nome o cognome
            return Regex.Replace(input, @"\s+", string.Empty).ToUpper();
        }
        // Verifica e corregge il calcolo del carattere di controllo
        private string CalcolaCarattereControllo(string codiceBase)
        {
            int sommaDispari = 0;
            int sommaPari = 0;

            // Itera sui primi 15 caratteri del codice fiscale
            for (int i = 0; i < 15; i++)
            {
                char carattere = codiceBase[i];

                if ((i + 1) % 2 == 0) // Posizione pari (indice informatico pari)
                {
                    sommaPari += TabellaPari(carattere);
                }
                else // Posizione dispari (indice informatico dispari)
                {
                    sommaDispari += TabellaDispari(carattere);
                }
            }

            // Calcola il totale e il resto della divisione per 26
            int totale = (sommaDispari + sommaPari) % 26;

            // Restituisce il carattere di controllo in base al resto
            return TabellaResto(totale).ToString();
        }
        // Tabella per i caratteri nelle posizioni dispari (secondo la Tabella D)
        private int TabellaDispari(char carattere)
        {
            var tabellaDispari = new Dictionary<char, int>
            {
                {'0', 1}, {'1', 0}, {'2', 5}, {'3', 7}, {'4', 9},
                {'5', 13}, {'6', 15}, {'7', 17}, {'8', 19}, {'9', 21},
                {'A', 1}, {'B', 0}, {'C', 5}, {'D', 7}, {'E', 9},
                {'F', 13}, {'G', 15}, {'H', 17}, {'I', 19}, {'J', 21},
                {'K', 2}, {'L', 4}, {'M', 18}, {'N', 20}, {'O', 11},
                {'P', 3}, {'Q', 6}, {'R', 8}, {'S', 12}, {'T', 14},
                {'U', 16}, {'V', 10}, {'W', 22}, {'X', 25}, {'Y', 24},
                {'Z', 23}
            };

            return tabellaDispari.ContainsKey(carattere) ? tabellaDispari[carattere] : 0; // Default 0 if character not found
        }

        // Tabella per i caratteri nelle posizioni pari (secondo la Tabella C)
        private int TabellaPari(char carattere)
        {
            var tabellaPari = new Dictionary<char, int>
            {
                {'0', 0}, {'1', 1}, {'2', 2}, {'3', 3}, {'4', 4},
                {'5', 5}, {'6', 6}, {'7', 7}, {'8', 8}, {'9', 9},
                {'A', 0}, {'B', 1}, {'C', 2}, {'D', 3}, {'E', 4},
                {'F', 5}, {'G', 6}, {'H', 7}, {'I', 8}, {'J', 9},
                {'K', 10}, {'L', 11}, {'M', 12}, {'N', 13}, {'O', 14},
                {'P', 15}, {'Q', 16}, {'R', 17}, {'S', 18}, {'T', 19},
                {'U', 20}, {'V', 21}, {'W', 22}, {'X', 23}, {'Y', 24},
                {'Z', 25}
            };

            return tabellaPari.ContainsKey(carattere) ? tabellaPari[carattere] : 0; // Default 0 if character not found
        }
        private char TabellaResto(int resto)
        {
            var tabellaResto = new Dictionary<int, char>
            {
                {0, 'A'}, {1, 'B'}, {2, 'C'}, {3, 'D'}, {4, 'E'},
                {5, 'F'}, {6, 'G'}, {7, 'H'}, {8, 'I'}, {9, 'J'},
                {10, 'K'}, {11, 'L'}, {12, 'M'}, {13, 'N'}, {14, 'O'},
                {15, 'P'}, {16, 'Q'}, {17, 'R'}, {18, 'S'}, {19, 'T'},
                {20, 'U'}, {21, 'V'}, {22, 'W'}, {23, 'X'}, {24, 'Y'},
                {25, 'Z'}
            };

            return tabellaResto.ContainsKey(resto) ? tabellaResto[resto] : 'A'; // Default 'A' if something goes wrong
        }

        private Dictionary<string, string> LoadComuniFromJson(string path)
        {
            var comuni = new Dictionary<string, string>();
            var json = System.IO.File.ReadAllText(path);
            var jsonArray = JArray.Parse(json);

            foreach (var item in jsonArray)
            {
                string nome = item["nome"].ToString().ToUpper();
                string codiceCatastale = item["codiceCatastale"].ToString();
                comuni[nome] = codiceCatastale;
            }

            return comuni;
        }
        private Dictionary<string, string> LoadElencoComuniItalianiFromJson(string path)
        {
            var comuni = new Dictionary<string, string>();

            var json = System.IO.File.ReadAllText(path);

            var jsonObject = JObject.Parse(json);

            // Accedi all'array contenuto nella proprietà "CODICI al 30-06-2024"
            var jsonArray = (JArray)jsonObject["CODICI al 30-06-2024"];

            foreach (var item in jsonArray)
            {
                string nomeComune = item["Denominazione in italiano"].ToString().ToUpper();
                string codiceCatastale = item["Codice Catastale del comune"].ToString();
                comuni[nomeComune] = codiceCatastale;
            }

            return comuni;
        }        
        private Dictionary<string, string> LoadElencoStatiEsteriFromJson(string path)
        {
            var comuni = new Dictionary<string, string>();

            var json = System.IO.File.ReadAllText(path);

            var jsonObject = JObject.Parse(json);

            // Accedi all'array contenuto nella proprietà "CODICI al 30-06-2024"
            var jsonArray = (JArray)jsonObject["NAZIONI"];

            foreach (var item in jsonArray)
            {
                string nomeComune = item["Nazione"].ToString().ToUpper();
                string codiceCatastale = item["Codice"].ToString();
                comuni[nomeComune] = codiceCatastale;
            }

            return comuni;
        }

    }
}