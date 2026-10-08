# AppFileTextProcessor

Raccolta di API per trasformare tracciati testuali, elaborare file Excel e applicare un logo ai PDF. Le operazioni sono accessibili da Swagger o da client HTTP e utilizzano file locali sul server oppure documenti caricati nella richiesta.

| Aspetto | Descrizione |
| --- | --- |
| Utilizzatore | Operatore tramite Swagger o client HTTP |
| Punto di ingresso | API REST con input locale o multipart |
| Risultato | File elaborati sul server oppure documenti scaricabili |

## Indice

- [Funzionalità](#funzionalità)
- [Tecnologie e requisiti](#tecnologie-e-requisiti)
- [Configurazione](#configurazione)
- [Avvio](#avvio)
- [Flussi operativi](#flussi-operativi)
- [Esempi di utilizzo](#esempi-di-utilizzo)
- [Verifiche](#verifiche)
- [Struttura e documentazione](#struttura-e-documentazione)

## Funzionalità

- Conversione dei tracciati APP, MIM e VSA.
- Elaborazione massiva Excel e separazione dei numeri telefonici.
- Unione e confronto di file Excel.
- Calcolo del codice fiscale da dati anagrafici.
- Sovrapposizione di un logo alle pagine PDF.

## Tecnologie e requisiti

**.NET 8**, ASP.NET Core Web API, EPPlus e NPOI per Excel, iText per PDF, SQL Server tramite SqlClient, Newtonsoft.Json, Swagger e Serilog.

Servono il .NET SDK compatibile con `net8.0` e una cartella locale scrivibile. Le conversioni che arricchiscono le anagrafiche richiedono SQL Server; le operazioni di unione Excel tramite upload non richiedono il database. L'operazione sui PDF richiede un file logo configurato.

## Configurazione

| Impostazione | Utilizzo |
| --- | --- |
| `FILE_PROCESSOR_DIRECTORY` | Variabile d'ambiente letta direttamente: directory di input/output locali |
| `ConnectionStrings:DefaultConnection_TENANT_A` | Connessione SQL per il primo tenant |
| `ConnectionStrings:DefaultConnection_TENANT_B` | Connessione SQL per il secondo tenant |
| `Branding:LogoPath` | Percorso del logo da sovrapporre ai PDF |

Se `FILE_PROCESSOR_DIRECTORY` non è impostata, viene usata la cartella `data` sotto la directory dell'applicazione compilata. Per evitare ambiguità è preferibile impostare un percorso assoluto.

Le impostazioni gerarchiche possono essere configurate con User Secrets o variabili d'ambiente usando `__` al posto di `:`. Ad esempio:

~~~powershell
dotnet user-secrets set --project AppFileTextProcessor/AppFileTextProcessor.csproj "Branding:LogoPath" "C:\dati-demo\logo.png"
~~~

Per usare i User Secrets avviare in ambiente Development.

### Contratti dei dati

Le query anagrafiche richiedono tabelle o viste coerenti con il servizio, fra cui `Pratiche` e i campi `AnnoProtocollo`, `CodiceFiscale`, `PartitaIva` e `Denominazione`. Alcune regole contengono intervalli annuali predefiniti, fra cui 2014–2024: verificare e adattare tali regole ai dati da elaborare.

I dizionari JSON sono nella cartella [json](AppFileTextProcessor/json) e vengono copiati negli output di compilazione/pubblicazione. Le regole di colonne e formati sono nei [controller](AppFileTextProcessor/Controllers) e nei [servizi](AppFileTextProcessor/Services).

## Avvio

Dalla radice del repository:

~~~powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:FILE_PROCESSOR_DIRECTORY = "C:\dati-demo\elaborazioni"
New-Item -ItemType Directory -Force $env:FILE_PROCESSOR_DIRECTORY
dotnet restore AppFileTextProcessor.sln
dotnet build AppFileTextProcessor.sln
dotnet run --project AppFileTextProcessor/AppFileTextProcessor.csproj --no-launch-profile --urls http://localhost:5118
~~~

Aprire `http://localhost:5118/`: **Swagger è alla radice**, non sotto `/swagger`. La specifica OpenAPI è su `/swagger/v1/swagger.json`.

## Flussi operativi

I flussi descrivono il comportamento implementato, inclusi gli effetti parziali e le automazioni non attive. La ricostruzione si basa sull'analisi statica dei sorgenti dell'8 ottobre 2026; le verifiche proposte non costituiscono test già eseguiti.

### Indice dei flussi

- [Contesto operativo](#contesto-operativo)
- [Conversioni di file locali](#conversioni-di-file-locali)
- [Upload Excel: unione, confronto e codice fiscale](#upload-excel-unione-confronto-e-codice-fiscale)
- [Upload PDF](#upload-pdf)
- [Diagramma dei due cicli](#diagramma-dei-due-cicli)
- [Errori e ripetizione](#errori-e-ripetizione)
- [Automazioni e attività dopo la risposta](#automazioni-e-attività-dopo-la-risposta)

### Contesto operativo

Il punto di ingresso è **Swagger alla radice dell'applicazione** oppure un client HTTP. Tutte le operazioni descritte utilizzano il metodo POST.

Le operazioni prevedono due modalità di ingresso:

- **File locali sul server:** l'utente prepara l'input nella directory `FILE_PROCESSOR_DIRECTORY` (fallback: `data` sotto `AppContext.BaseDirectory`). La risposta HTTP è un messaggio; l'output resta su disco sul server.
- **Upload:** l'utente sceglie i file in Swagger oppure invia multipart/form-data. La risposta contiene il documento scaricabile, elaborato in memoria.

Non è presente una coda di job: i controller completano l'elaborazione prima di restituire la risposta. Un metodo `async` non implica che il lavoro prosegua dopo la risposta.

### Conversioni di file locali

| Operazione e richiesta | Input e azione iniziale | Elaborazione automatica | Risposta e output |
| --- | --- | --- | --- |
| `/api/AppTextConvert/process?outputFileName=...` | Preparare APP_INIZIALE.txt e indicare il nome output senza estensione | Legge UTF-8; normalizza testo, crea sezioni per anno, elimina alcune righe tecniche, riconosce comuni e CF/P.IVA; cerca denominazioni SQL prima nel tenant A poi nel B se non trovate | 200 con messaggio; salva nome scelto + .txt |
| `/api/MimTextToExcel/process` | Preparare MIM_INIZIALE.txt | Legge ISO-8859-1; riconosce inizio record e accumula l'esito fino al record successivo; esporta Protocollo, Identificativo, ESITO | 200 con messaggio; MIM_FINALE.xlsx |
| `/api/VsaTextToExcel/process` | Preparare VSA_INIZIALE.txt | Stesso parser testuale e stesso servizio di esportazione di MIM | 200 con messaggio; VSA_FINALE.xlsx |
| `/api/VsaMassExcel/process` | Preparare VSA_MASS.xlsx con il tracciato atteso | Legge il primo foglio, costruisce un esito descrittivo per riga, include il rappresentante quando le colonne 21–36 non sono tutte vuote | 200 con messaggio; VSA_MASS_FINALE.xlsx |
| `/api/PhoneNumberProcessor/process?inputFileName=...&outputFileName=...` | Preparare input .xlsx e indicare entrambi i nomi senza estensione | Legge Foglio1, colonna 22; separa numeri per spazio/virgola, deduplica, classifica per prefisso 0 o 3 e scrive le colonne 22–25 | 200 con messaggio; nome output + .xls |

**Ciclo dell'utente:** prepara input → esegue POST → attende → legge il messaggio → recupera l'output dalla cartella server → controlla il contenuto. Non è previsto un download HTTP per queste cinque operazioni.

Riferimenti: [APP](AppFileTextProcessor/Controllers/AppTextConverterController.cs), [MIM](AppFileTextProcessor/Controllers/MimTextToExcelController.cs), [VSA](AppFileTextProcessor/Controllers/VsaTextToExcelController.cs), [VSA massivo](AppFileTextProcessor/Controllers/VsaMassExcelController.cs), [telefoni](AppFileTextProcessor/Controllers/PhoneNumberProcessorController.cs).

#### Regole che cambiano il risultato

- Il parser MIM/VSA riconosce protocolli che iniziano con **l'anno corrente del server seguito da sette cifre**. Testi con altri anni possono produrre un output senza i record attesi, pur con risposta 200.
- APP cerca denominazioni nelle pratiche con anni **2014–2024**. Se non trova la denominazione, conserva l'identificativo tra parentesi.
- Nei telefoni, vuoto o “NON RISALIBILE” produce quella dicitura in colonna 22 e svuota le altre tre. La classificazione è per prefisso; non certifica la validità del numero. La seconda posizione di ogni tipo viene sovrascritta da ulteriori numeri dello stesso tipo.
- L'output telefoni è denominato `.xls`, ma viene scritto tramite EPPlus: il nome non prova che sia un file binario legacy XLS.
- I nomi locali fissi possono sovrascrivere output precedenti. Non è implementato un archivio versionato o un rollback dei file.

### Upload Excel: unione, confronto e codice fiscale

L'utente apre l'endpoint, seleziona gli allegati richiesti e invia la richiesta. Il controller apre il primo foglio, applica le regole e restituisce un file. Dopo la risposta, l'utente salva il download e controlla il risultato; non parte un invio esterno.

| Endpoint e campi multipart | Passaggi di elaborazione | Risultato |
| --- | --- | --- |
| `/api/ExcelMerger/merge`: file1, file2 | Copia l'intestazione del primo file, accoda le righe dei due fogli e mescola le righe con Random | Download XLSX; ordine non stabile tra esecuzioni |
| `/api/MergerExcelFiles/merge`: fileTENANT_B, fileTENANT_A | Copia intestazione e numero di colonne da B, accoda prima B poi A, formatta e adatta larghezze | XLSX con nome derivato da B e dal totale righe |
| `/api/ConfrontaFile/confronta`: fileA, fileB | Indicizza B per CF in colonna 2, legge stato in 4 ed email in 5; cerca i CF di A in colonna 2 e aggiorna le colonne **50 (AX)** e **51 (AY)** | LISTA_NIS2_aggiornato.xlsx |
| `/api/CodiceFiscale/process`: file | Legge cognome, nome, sesso, data, luogo e provincia nelle prime sei colonne; calcola CF e carattere di controllo usando dizionari JSON geografici | Codici_Fiscali_Elaborati.xlsx; risultato in colonna 7 |

Il nome della route `MergerExcelFiles` deriva dalla classe, anche se il sorgente si chiama [MergeExcelFilesController.cs](AppFileTextProcessor/Controllers/MergeExcelFilesController.cs).

Nel confronto, CF ripetuti in B conservano l'ultima coppia stato/email; righe non corrispondenti di A restano invariate. Le lettere delle colonne nei commenti del codice non corrispondono agli indici: la documentazione usa gli indici realmente eseguiti.

Nel calcolo CF, una data non leggibile come `dd/MM/yyyy` produce un messaggio nella cella e la lavorazione continua sulle altre righe; un luogo non trovato produce “Comune non trovato”. La risposta può quindi essere 200 con errori per riga. Il parametro provincia viene letto, ma la ricerca catastale implementata usa il nome del luogo. Non è presente una verifica presso un servizio anagrafico esterno.

Riferimenti: [unione mescolata](AppFileTextProcessor/Controllers/ExcelMergerController.cs), [unione tenant](AppFileTextProcessor/Controllers/MergeExcelFilesController.cs), [confronto](AppFileTextProcessor/Controllers/ConfrontaFileController.cs), [CF](AppFileTextProcessor/Controllers/CodiceFiscaleController.cs).

### Upload PDF

1. L'utente seleziona un PDF nel campo `File` di `POST /api/PdfProcessing/replace-logo`.
2. Il controller rifiuta file assente o vuoto con 400.
3. Copia l'upload in memoria e chiama `PdfProcessingService.ReplaceLogo`.
4. Il servizio apre il PDF e carica il logo locale configurato in `Branding:LogoPath`.
5. Ridimensiona il logo a larghezza 150 e lo sovrappone in alto a destra di ogni pagina, con margine 25.
6. Chiude il documento e restituisce i byte; il controller risponde con `modified.pdf`.
7. L'utente scarica e verifica tutte le pagine.

**Comportamento implementato:** il logo è sovrapposto; il contenuto originale sottostante non viene rimosso. Logo mancante, PDF non leggibile o errore di elaborazione producono 500. Riferimenti: [controller PDF](AppFileTextProcessor/Controllers/PdfprocessingController.cs), [servizio PDF](AppFileTextProcessor/Services/PdfProcessingService.cs).

### Diagramma dei due cicli

```mermaid
flowchart TD
    A["Swagger o client HTTP"] --> B{"Tipo di ingresso"}
    B -->|Locale| C["Prepara file sul server"]
    B -->|Upload| D["Seleziona allegati"]
    C --> E["POST e validazione"]
    D --> E
    E --> F{"Input utilizzabile?"}
    F -->|No| G["Risposta di errore"]
    F -->|Sì| H["Elaborazione nella richiesta"]
    H --> I{"Destinazione"}
    I -->|Disco| J["Salva output e risponde con messaggio"]
    I -->|Download| K["Restituisce documento"]
    J --> L["Utente recupera e controlla"]
    K --> L
    H -->|Eccezione| G
```

### Errori e ripetizione

| Evento | Esito osservabile |
| --- | --- |
| Input locale mancante | 400 nelle conversioni APP/MIM/VSA/massivo; 404 nei telefoni |
| Nomi query richiesti assenti | 400 |
| Allegati assenti | 400; i controlli su file vuoti variano tra controller |
| Foglio, tracciato, JSON, logo o connessione SQL non utilizzabile | Eccezione; in genere 500 dal controller. Errori durante la costruzione delle dipendenze possono avvenire prima del suo try/catch |
| Operazione completata | 200 con messaggio o file, secondo l'operazione richiesta |
| Ripetizione | Ricalcola il risultato; nei file locali può sovrascrivere l'output, nell'unione mescolata può cambiare l'ordine |

Non è previsto un tentativo automatico applicativo dopo un errore. Una risposta HTTP 200 indica il completamento dell'elaborazione. La conformità del contenuto al tracciato richiede un controllo del risultato.

### Automazioni e attività dopo la risposta

Le trasformazioni, le ricerche SQL, la scelta dei dizionari, il calcolo e la generazione file sono automatici **dentro la richiesta**. Logging Serilog registra l'attività. `AtecoService` è presente e registrato, ma non risulta un endpoint dedicato né una chiamata ai suoi metodi nelle elaborazioni esaminate: non costituisce quindi un flusso utente attivo.

Non risultano scheduler, watcher, notifiche email, code persistenti, trasferimenti automatici o workflow GitHub Actions. Dopo la risposta non resta un job di elaborazione attivo.

## Esempi di utilizzo

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

## Verifiche

### Verifiche funzionali consigliate

Per ciascun endpoint provare un input conforme, un input assente e un tracciato non conforme. Per i locali controllare il file sul server; per gli upload aprire il download. Verificare in particolare anno dei protocolli MIM/VSA, colonne AX/AY del confronto, errori per riga del CF, sovrapposizione PDF e ripetizione su un output locale esistente.

## Struttura e documentazione

- [Controller](AppFileTextProcessor/Controllers): operazioni HTTP.
- [Servizi](AppFileTextProcessor/Services): trasformazioni e accesso ai dati.
- [json](AppFileTextProcessor/json): dizionari inclusi.
- [FLUSSI.md](FLUSSI.md): versione dedicata dei flussi riportati integralmente in questo README.
