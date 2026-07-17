using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.BulkImports.Models;
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
