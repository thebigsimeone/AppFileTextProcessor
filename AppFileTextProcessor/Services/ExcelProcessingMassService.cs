using AppFileTextProcessor.Interface;
using OfficeOpenXml;
using System.Text;

namespace AppFileTextProcessor.Services
{
    public class ExcelProcessingMassService : IExcelProcessingMassService
    {
        private readonly IExcelExportService _excelExportService;

        public ExcelProcessingMassService(IExcelExportService excelExportService)
        {
            _excelExportService = excelExportService;
        }

        public List<(string Protocollo, string Identificativo, string Esito)> ProcessExcel(string inputFilePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var resultData = new List<(string Protocollo, string Identificativo, string Esito)>();

            using (var package = new ExcelPackage(new FileInfo(inputFilePath)))
            {
                var worksheet = package.Workbook.Worksheets[0];

                int totalRows = worksheet.Dimension.Rows;

                for (int row = 2; row <= totalRows; row++)
                {
                    string protocollo = worksheet.Cells[row, 1].Text.Trim();
                    string codiceFiscale = worksheet.Cells[row, 2].Text.Trim();
                    string cf = worksheet.Cells[row, 3].Text.Trim();
                    string partitaIva = worksheet.Cells[row, 4].Text.Trim();
                    string cognome = worksheet.Cells[row, 5].Text.Trim();
                    string nome = worksheet.Cells[row, 6].Text.Trim();
                    string indirizzo = worksheet.Cells[row, 7].Text.Trim();
                    string cap = worksheet.Cells[row, 8].Text.Trim();
                    string comune = worksheet.Cells[row, 9].Text.Trim();
                    string pr = worksheet.Cells[row, 10].Text.Trim();
                    string statoPiva = worksheet.Cells[row, 12].Text.Trim();
                    string descrizioneAttivita = worksheet.Cells[row, 13].Text.Trim();
                    string codiceAttivita = worksheet.Cells[row, 14].Text.Trim();
                    string inizioAttivita = worksheet.Cells[row, 15].Text.Trim();
                    string fineAttivita = worksheet.Cells[row, 16].Text.Trim();
                    string comuneSedeLegale = worksheet.Cells[row, 17].Text.Trim();
                    string provSedeLegale = worksheet.Cells[row, 18].Text.Trim();
                    string capSedeLegale = worksheet.Cells[row, 19].Text.Trim();
                    string indirizzoSedeLegale = worksheet.Cells[row, 20].Text.Trim();

                    // Colonne rappresentante U -> AJ (21 -> 36)
                    bool isDittaIndividuale = Enumerable.Range(21, 16).All(col => string.IsNullOrWhiteSpace(worksheet.Cells[row, col].Text));

                    string identificativo = codiceFiscale;

                    StringBuilder esitoBuilder = new StringBuilder();

                    if (isDittaIndividuale)
                    {
                        esitoBuilder.AppendLine($"{cognome} {nome} Codice fiscale {cf}");
                        esitoBuilder.AppendLine($"Domiciliato in {indirizzo} {cap} {comune} ({pr})");
                        esitoBuilder.AppendLine();
                    }

                    esitoBuilder.AppendLine("Dati identificativi della ditta individuale");
                    esitoBuilder.AppendLine($"Denominazione {cognome} {nome}");
                    esitoBuilder.AppendLine($"Partita IVA {partitaIva} attribuita il {inizioAttivita}");
                    esitoBuilder.AppendLine($"Stato {statoPiva}");
                    esitoBuilder.AppendLine($"Attività esercitata {descrizioneAttivita} ({codiceAttivita})");
                    esitoBuilder.AppendLine($"Luogo di esercizio in {indirizzoSedeLegale} - {comuneSedeLegale} ({provSedeLegale}) - {capSedeLegale}");

                    if (!string.IsNullOrWhiteSpace(fineAttivita))
                    {
                        esitoBuilder.AppendLine($"a decorrere dal {inizioAttivita} al {fineAttivita}");
                    }
                    else
                    {
                        esitoBuilder.AppendLine($"a decorrere dal {inizioAttivita}");
                    }


                    if (!isDittaIndividuale)
                    {
                        string inizioDataDecorrere = worksheet.Cells[row, 23].Text.Trim();
                        string fineDataDecorrere = worksheet.Cells[row, 24].Text.Trim();
                        string cfRapp = worksheet.Cells[row, 25].Text.Trim();
                        string cognomeRapp = worksheet.Cells[row, 26].Text.Trim();
                        string nomeRapp = worksheet.Cells[row, 27].Text.Trim();
                        string comuneNascita = worksheet.Cells[row, 30].Text.Trim();
                        string provNascita = worksheet.Cells[row, 29].Text.Trim();
                        string dataNascita = worksheet.Cells[row, 31].Text.Trim();
                        string indirizzoResidenza = worksheet.Cells[row, 33].Text.Trim();
                        string capResidenza = worksheet.Cells[row, 34].Text.Trim();
                        string comuneResidenza = worksheet.Cells[row, 35].Text.Trim();
                        string provResidenza = worksheet.Cells[row, 36].Text.Trim();

                        esitoBuilder.AppendLine();
                        esitoBuilder.AppendLine($"Rappresentante legale {cognomeRapp} {nomeRapp} Codice fiscale {cfRapp}");
                        if (!string.IsNullOrWhiteSpace(fineDataDecorrere))
                        {
                            esitoBuilder.AppendLine($"a decorrere dal {inizioDataDecorrere} al {fineDataDecorrere}");
                        }
                        else
                        {
                            esitoBuilder.AppendLine($"a decorrere dal {inizioDataDecorrere}");
                        }
                        esitoBuilder.AppendLine($"nato a {comuneNascita} ({provNascita}) il {dataNascita}");
                        esitoBuilder.AppendLine($"Residente in {indirizzoResidenza} {capResidenza} {comuneResidenza} ({provResidenza})");
                    }

                    resultData.Add((protocollo, identificativo, esitoBuilder.ToString().Trim()));
                }
            }

            return resultData;
        }

        public void ProcessAndSaveExcel(string inputFilePath, string outputFilePath)
        {
            var data = ProcessExcel(inputFilePath);
            _excelExportService.SaveToExcel(data, outputFilePath);
        }
    }
}
