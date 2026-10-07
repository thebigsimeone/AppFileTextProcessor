# AppFileTextProcessor

Raccolta di strumenti HTTP per trasformare file di testo, elaborare Excel e applicare un logo a PDF. Le operazioni sono esposte come API e possono essere provate dall'interfaccia Swagger inclusa.

È pensata per tracciati operativi specifici: conversioni APP, MIM e VSA, arricchimento anagrafico, unione di fogli, elaborazione di numeri telefonici e calcolo del codice fiscale.

## Tecnologie e requisiti

**.NET 8**, ASP.NET Core Web API, EPPlus e NPOI per Excel, iText per PDF, SQL Server tramite SqlClient, Newtonsoft.Json, Swagger e Serilog.

Servono il .NET SDK compatibile con `net8.0` e una cartella locale scrivibile. Le conversioni che arricchiscono le anagrafiche richiedono SQL Server; i semplici upload per unione Excel non richiedono il database. L'operazione sui PDF richiede un file logo configurato.

## Configurazione

| Impostazione | Utilizzo |
| --- | --- |
| `FILE_PROCESSOR_DIRECTORY` | Variabile d'ambiente letta direttamente: directory di input/output locali |
| `ConnectionStrings:DefaultConnection_TENANT_A` | Connessione SQL per il primo tenant |
| `ConnectionStrings:DefaultConnection_TENANT_B` | Connessione SQL per il secondo tenant |
| `Branding:LogoPath` | Percorso del logo da sovrapporre ai PDF |

Se `FILE_PROCESSOR_DIRECTORY` non è impostata, viene usata la cartella `data` sotto la directory dell'applicazione compilata. Per evitare ambiguità è preferibile impostare un percorso assoluto.

Esempio PowerShell, dalla radice del repository:

~~~powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:FILE_PROCESSOR_DIRECTORY = "C:\dati-demo\elaborazioni"
New-Item -ItemType Directory -Force $env:FILE_PROCESSOR_DIRECTORY
dotnet restore AppFileTextProcessor.sln
dotnet build AppFileTextProcessor.sln
dotnet run --project AppFileTextProcessor/AppFileTextProcessor.csproj --no-launch-profile --urls http://localhost:5118
~~~

Aprire `http://localhost:5118/`: **Swagger è alla radice**, non sotto `/swagger`. La specifica OpenAPI è su `/swagger/v1/swagger.json`.

Le impostazioni gerarchiche possono essere configurate con User Secrets o variabili d'ambiente usando `__` al posto di `:`. Ad esempio:

~~~powershell
dotnet user-secrets set --project AppFileTextProcessor/AppFileTextProcessor.csproj "Branding:LogoPath" "C:\dati-demo\logo.png"
~~~

Per usare i User Secrets avviare in ambiente Development.

## Operazioni disponibili

Tutte le operazioni indicate usano il metodo **POST**.

| Endpoint | Ingresso | Risultato |
| --- | --- | --- |
| `/api/AppTextConvert/process` | File locale `APP_INIZIALE.txt` e `outputFileName` senza estensione | Testo elaborato con arricchimento dei dati |
| `/api/MimTextToExcel/process` | File locale `MIM_INIZIALE.txt` | `MIM_FINALE.xlsx` |
| `/api/VsaTextToExcel/process` | File locale `VSA_INIZIALE.txt` | `VSA_FINALE.xlsx` |
| `/api/VsaMassExcel/process` | File locale `VSA_MASS.xlsx` | `VSA_MASS_FINALE.xlsx` |
| `/api/PhoneNumberProcessor/process` | Parametri `inputFileName` e `outputFileName` senza estensione | Input `.xlsx`, output denominato `.xls`, con elaborazione delle colonne telefoniche previste |
| `/api/ExcelMerger/merge` | Upload `file1` e `file2` | Excel unito con righe mescolate |
| `/api/MergerExcelFiles/merge` | Upload `fileTENANT_B` e `fileTENANT_A` | Excel unito mantenendo le intestazioni |
| `/api/ConfrontaFile/confronta` | Upload `fileA` e `fileB` | Confronto e aggiornamento dei campi previsti dal tracciato |
| `/api/CodiceFiscale/process` | Upload `file` | Excel con codice fiscale calcolato |
| `/api/PdfProcessing/replace-logo` | Upload `File` | PDF con logo sovrapposto |

Per i processori locali, mettere prima il file nella directory configurata. Per le operazioni di upload, inviare il file come `multipart/form-data` tramite Swagger o un client HTTP.

### Esempio: unire due Excel

I primi fogli dei due file devono avere colonne compatibili e l'intestazione in prima riga.

~~~powershell
curl.exe --fail-with-body -X POST "http://localhost:5118/api/ExcelMerger/merge" -F "file1=@C:\dati-demo\primo.xlsx" -F "file2=@C:\dati-demo\secondo.xlsx" --output "unione.xlsx"
~~~

Questo endpoint copia l'intestazione dal primo file, accoda i dati di entrambi e mescola le righe.

### Esempio: conversione di un tracciato locale

1. Copiare `MIM_INIZIALE.txt` nella cartella indicata da `FILE_PROCESSOR_DIRECTORY`.
2. Controllare che il contenuto rispetti il tracciato atteso dal servizio.
3. Eseguire `POST /api/MimTextToExcel/process` da Swagger.
4. Verificare il risultato `MIM_FINALE.xlsx`.

I tracciati MIM/VSA sono letti con codifica ISO-8859-1; APP usa UTF-8. Non basta rinominare un testo generico per renderlo compatibile con una conversione.

### Calcolo del codice fiscale

Il primo foglio deve contenere un'intestazione e queste sei colonne, nell'ordine:

| Colonna | Contenuto |
| --- | --- |
| 1 | Cognome |
| 2 | Nome |
| 3 | Sesso |
| 4 | Data di nascita, `dd/MM/yyyy` |
| 5 | Comune o luogo di nascita |
| 6 | Provincia |

Il risultato viene scritto nella settima colonna, con messaggi di errore dove il dato non è elaborabile. Il calcolo usa i dizionari geografici inclusi: non verifica il codice presso un'anagrafe ufficiale e non risolve automaticamente tutti i casi particolari.

### Elaborazione PDF

Impostare `Branding:LogoPath` e inviare il PDF al relativo endpoint. Il servizio aggiunge un'immagine in alto a destra sulle pagine. La sovrapposizione **non cancella il contenuto originale sottostante** e non costituisce un metodo di oscuramento dei dati.

## Dati e limiti dei tracciati

Le query anagrafiche richiedono tabelle o viste coerenti con il servizio, fra cui `Pratiche` e i campi `AnnoProtocollo`, `CodiceFiscale`, `PartitaIva` e `Denominazione`. Alcune regole contengono intervalli annuali predefiniti, fra cui 2014–2024: verificare e adattare tali regole ai dati da elaborare.

I dizionari JSON sono nella cartella [json](AppFileTextProcessor/json) e vengono copiati negli output di compilazione/pubblicazione. Le regole di colonne e formati sono nei [controller](AppFileTextProcessor/Controllers) e nei [servizi](AppFileTextProcessor/Services).

Gli endpoint elaborano e possono sovrascrivere file locali. Usare copie dei documenti originali e una cartella di lavoro dedicata. Per la configurazione esterna e i dati esclusi dal repository vedere [PUBLICATION.md](PUBLICATION.md).
