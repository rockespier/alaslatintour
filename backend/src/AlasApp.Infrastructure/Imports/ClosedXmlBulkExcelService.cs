using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.BulkImports.Models;
using AlasApp.Application.EventResults.Models;
using AlasApp.Application.Inscriptions.Models;
using ClosedXML.Excel;

namespace AlasApp.Infrastructure.Imports;

public sealed class ClosedXmlBulkExcelService : IBulkExcelService
{
    private static readonly string[] CircuitHeaders =
    [
        "Id",
        "SurfScoresCode",
        "Nombre",
        "Temporada",
        "Descripcion",
        "Region",
        "Modalidad",
        "Estado"
    ];

    private static readonly string[] EventHeaders =
    [
        "Id",
        "SurfScoresCode",
        "CircuitId",
        "CircuitSurfScoresCode",
        "Nombre",
        "FechaInicio",
        "FechaFin",
        "Pais",
        "Ciudad",
        "Playa",
        "Auspiciador",
        "ImagenUrl",
        "Stars",
        "CapacidadMaxima",
        "PrizeAmountUsd",
        "EventType",
        "AccessType",
        "Estado"
    ];

    private static readonly string[] CategoryHeaders =
    [
        "Id",
        "SurfScoresCode",
        "Nombre",
        "Descripcion",
        "Gender",
        "AgeRestriction",
        "MinAge",
        "MaxAge",
        "SuccessorCategoryId",
        "SuccessorSurfScoresCode",
        "Status",
        "MembresiaAnualUsd",
        "MembresiaPorEventoUsd",
        "BestResultsCount"
    ];

    public byte[] BuildCircuitsTemplate()
        => BuildWorkbook("Circuits", CircuitHeaders, ["", "ALAS-2026", "ALAS Global Tour", "2026", "Circuito principal", "Latinoamerica", "Shortboard", "Activo"]);

    public byte[] BuildEventsTemplate()
        => BuildWorkbook("Events", EventHeaders, ["", "MANCORA-2026", "", "ALAS-2026", "Mancora Pro", "2026-08-10", "2026-08-13", "Peru", "Mancora", "Playa Pocitas", "Marca X", "https://cdn.test/event.png", "6", "120", "5000.00", "Prime", "Abierto", "Activo"]);

    public byte[] BuildCategoriesTemplate()
        => BuildWorkbook("Categories", CategoryHeaders, ["", "OPEN-MEN", "Open Masculino", "Categoria principal", "Masculino", "false", "", "", "", "", "Activo", "35.00", "12.00", "5"]);

    public IReadOnlyCollection<CircuitImportRow> ReadCircuits(byte[] content)
        => ReadRows(content, "Circuits", CircuitHeaders, values => new CircuitImportRow(
            values.RowNumber,
            values["Id"],
            values["SurfScoresCode"],
            values["Nombre"],
            values["Temporada"],
            values["Descripcion"],
            values["Region"],
            values["Modalidad"],
            values["Estado"]));

    public IReadOnlyCollection<EventImportRow> ReadEvents(byte[] content)
        => ReadRows(content, "Events", EventHeaders, values => new EventImportRow(
            values.RowNumber,
            values["Id"],
            values["SurfScoresCode"],
            values["CircuitId"],
            values["CircuitSurfScoresCode"],
            values["Nombre"],
            values["FechaInicio"],
            values["FechaFin"],
            values["Pais"],
            values["Ciudad"],
            values["Playa"],
            values["Auspiciador"],
            values["ImagenUrl"],
            values["Stars"],
            values["CapacidadMaxima"],
            values["PrizeAmountUsd"],
            values["EventType"],
            values["AccessType"],
            values["Estado"]));

    public IReadOnlyCollection<CategoryImportRow> ReadCategories(byte[] content)
        => ReadRows(content, "Categories", CategoryHeaders, values => new CategoryImportRow(
            values.RowNumber,
            values["Id"],
            values["SurfScoresCode"],
            values["Nombre"],
            values["Descripcion"],
            values["Gender"],
            values["AgeRestriction"],
            values["MinAge"],
            values["MaxAge"],
            values["SuccessorCategoryId"],
            values["SuccessorSurfScoresCode"],
            values["Status"],
            values["MembresiaAnualUsd"],
            values["MembresiaPorEventoUsd"],
            values["BestResultsCount"]));

    private static readonly string[] InscriptionExportHeaders =
    [
        "Numero",
        "Competidor",
        "Pais",
        "Categoria",
        "Evento",
        "Fecha Inscripcion",
        "Metodo de Pago",
        "Monto USD",
        "Estado",
        "Federacion",
        "Licencia",
        "Transaccion"
    ];

    public byte[] BuildInscriptionsExport(IReadOnlyCollection<AdminInscriptionRowDto> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Inscritos");

        for (var index = 0; index < InscriptionExportHeaders.Length; index++)
        {
            worksheet.Cell(1, index + 1).Value = InscriptionExportHeaders[index];
            worksheet.Cell(1, index + 1).Style.Font.Bold = true;
        }

        var rowNumber = 2;
        foreach (var row in rows)
        {
            worksheet.Cell(rowNumber, 1).Value = row.SequentialNumber;
            worksheet.Cell(rowNumber, 2).Value = row.FullName;
            worksheet.Cell(rowNumber, 3).Value = row.Country;
            worksheet.Cell(rowNumber, 4).Value = row.Categoria;
            worksheet.Cell(rowNumber, 5).Value = row.EventoNombre;
            worksheet.Cell(rowNumber, 6).Value = row.InscripcionDate.ToString("yyyy-MM-dd HH:mm");
            worksheet.Cell(rowNumber, 7).Value = row.PaymentMethod.ToString();
            worksheet.Cell(rowNumber, 8).Value = (double)row.MontoUsd;
            worksheet.Cell(rowNumber, 9).Value = row.EstadoAdmin.ToString();
            worksheet.Cell(rowNumber, 10).Value = row.Federacion;
            worksheet.Cell(rowNumber, 11).Value = row.LicenciaNumber;
            worksheet.Cell(rowNumber, 12).Value = row.TransaccionId ?? string.Empty;
            rowNumber++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] BuildInscriptionFicha(InscriptionDto inscription)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Ficha");

        var fields = new (string Label, string Value)[]
        {
            ("Competidor", inscription.Competitor.FullName),
            ("Pais", inscription.Competitor.Country),
            ("Evento", inscription.Event.Nombre),
            ("Lugar", inscription.Event.Lugar),
            ("Categoria", inscription.Category.Nombre),
            ("Circuito", inscription.Circuit.Nombre),
            ("Metodo de Pago", inscription.PaymentMethod.ToString()),
            ("Monto USD", inscription.MontoUsd.ToString("0.00")),
            ("Estado Administrativo", inscription.EstadoAdmin.ToString()),
            ("Estado del Competidor", inscription.EstadoCompetidor.ToString()),
            ("Transaccion", inscription.TransaccionId ?? string.Empty),
            ("Fecha de Inscripcion", inscription.InscripcionAt.ToString("yyyy-MM-dd HH:mm")),
            ("Notas", inscription.Notes ?? string.Empty)
        };

        for (var index = 0; index < fields.Length; index++)
        {
            worksheet.Cell(index + 1, 1).Value = fields[index].Label;
            worksheet.Cell(index + 1, 1).Style.Font.Bold = true;
            worksheet.Cell(index + 1, 2).Value = fields[index].Value;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static readonly string[] EventResultHeaders =
    [
        "CompetidorId",
        "Competidor",
        "Pais",
        "Puesto",
        "PuntosLiga",
        "PremioUsd",
        "HeatOla1",
        "HeatOla2"
    ];

    public byte[] BuildEventResultsTemplate(IReadOnlyCollection<EventResultRosterRowDto> roster)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Resultados");

        for (var index = 0; index < EventResultHeaders.Length; index++)
        {
            worksheet.Cell(1, index + 1).Value = EventResultHeaders[index];
            worksheet.Cell(1, index + 1).Style.Font.Bold = true;
        }

        var rowNumber = 2;
        foreach (var row in roster)
        {
            worksheet.Cell(rowNumber, 1).Value = row.CompetitorId.ToString();
            worksheet.Cell(rowNumber, 2).Value = row.CompetitorName;
            worksheet.Cell(rowNumber, 3).Value = row.Country;
            worksheet.Cell(rowNumber, 4).Value = row.Place ?? string.Empty;
            worksheet.Cell(rowNumber, 5).Value = row.LigaPoints?.ToString() ?? string.Empty;
            worksheet.Cell(rowNumber, 6).Value = row.PrizeUsd?.ToString("0.00") ?? string.Empty;
            worksheet.Cell(rowNumber, 7).Value = row.HeatOla1?.ToString("0.00") ?? string.Empty;
            worksheet.Cell(rowNumber, 8).Value = row.HeatOla2?.ToString("0.00") ?? string.Empty;
            rowNumber++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public IReadOnlyCollection<EventResultImportRow> ReadEventResults(byte[] content)
        => ReadRows(content, "Resultados", EventResultHeaders, values => new EventResultImportRow(
            values.RowNumber,
            values["CompetidorId"],
            values["Competidor"],
            values["Puesto"],
            values["PuntosLiga"],
            values["PremioUsd"],
            values["HeatOla1"],
            values["HeatOla2"]));

    private static byte[] BuildWorkbook(string sheetName, IReadOnlyList<string> headers, IReadOnlyList<string> sampleRow)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        for (var index = 0; index < headers.Count; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
            worksheet.Cell(1, index + 1).Style.Font.Bold = true;
            worksheet.Cell(2, index + 1).Value = sampleRow[index];
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static IReadOnlyCollection<T> ReadRows<T>(
        byte[] content,
        string sheetName,
        IReadOnlyList<string> headers,
        Func<RowValues, T> map)
    {
        using var stream = new MemoryStream(content);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet(sheetName);
        var rows = new List<T>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            if (row.Cells(1, headers.Count).All(cell => cell.IsEmpty()))
            {
                continue;
            }

            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < headers.Count; index++)
            {
                values[headers[index]] = row.Cell(index + 1).GetString()?.Trim();
            }

            rows.Add(map(new RowValues(rowNumber, values)));
        }

        return rows;
    }

    private sealed record RowValues(int RowNumber, IReadOnlyDictionary<string, string?> Values)
    {
        public string? this[string key] => Values.TryGetValue(key, out var value) ? value : null;
    }
}
