# Configurazione privata

Le credenziali e i dati operativi devono restare fuori dal repository. Le impostazioni ASP.NET Core si configurano tramite variabili di ambiente (separatore doppio underscore) oppure dotnet user-secrets in sviluppo, indicando il progetto con --project. I valori vuoti nei file di esempio devono essere configurati prima di utilizzare i servizi corrispondenti. I file .env non vengono caricati automaticamente.

Non includere backup, esportazioni, log, chiavi private o dati personali nei commit.

La bonifica dei file correnti non elimina i valori presenti nella cronologia Git: prima di rendere pubblico il repository, revocare o ruotare le credenziali già versionate e bonificare la cronologia, gli altri branch/tag e gli eventuali allegati.

Configurare ConnectionStrings__DefaultConnection_TENANT_A, ConnectionStrings__DefaultConnection_TENANT_B e Branding__LogoPath (file locale privato). Logo aziendale e font personalizzato non sono distribuiti nel repository bonificato.

FILE_PROCESSOR_DIRECTORY seleziona la directory privata dei file di input/output (default: data nella directory dell’applicazione). I dizionari geografici JSON sono caricati dalla sottocartella json accanto all’eseguibile. La ricerca anagrafica usa lo schema dimostrativo Pratiche con colonne AnnoProtocollo, CodiceFiscale, PartitaIva e Denominazione, coerente con CreazioneListe.
