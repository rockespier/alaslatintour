using AlasApp.Application.BulkImports.Models;

namespace AlasApp.Application.Abstractions.Services;

public interface IBulkExcelService
{
    byte[] BuildCircuitsTemplate();

    byte[] BuildEventsTemplate();

    byte[] BuildCategoriesTemplate();

    IReadOnlyCollection<CircuitImportRow> ReadCircuits(byte[] content);

    IReadOnlyCollection<EventImportRow> ReadEvents(byte[] content);

    IReadOnlyCollection<CategoryImportRow> ReadCategories(byte[] content);
}
