using AlasApp.Application.BulkImports.Models;
using AlasApp.Application.EventResults.Models;
using AlasApp.Application.Inscriptions.Models;
using AlasApp.Application.Payments.Models;

namespace AlasApp.Application.Abstractions.Services;

public interface IBulkExcelService
{
    byte[] BuildCircuitsTemplate();

    byte[] BuildEventsTemplate();

    byte[] BuildCategoriesTemplate();

    byte[] BuildCompetitorsTemplate();

    IReadOnlyCollection<CircuitImportRow> ReadCircuits(byte[] content);

    IReadOnlyCollection<EventImportRow> ReadEvents(byte[] content);

    IReadOnlyCollection<CategoryImportRow> ReadCategories(byte[] content);

    IReadOnlyCollection<CompetitorImportRow> ReadCompetitors(byte[] content);

    byte[] BuildInscriptionsExport(IReadOnlyCollection<AdminInscriptionRowDto> rows);

    byte[] BuildPaymentsExport(IReadOnlyCollection<PaymentDto> rows);

    byte[] BuildInscriptionFicha(InscriptionDto inscription);

    byte[] BuildEventResultsTemplate(IReadOnlyCollection<EventResultRosterRowDto> roster);

    IReadOnlyCollection<EventResultImportRow> ReadEventResults(byte[] content);

    byte[] BuildInscriptionsTemplate();

    IReadOnlyCollection<InscriptionImportRow> ReadInscriptions(byte[] content);

    byte[] BuildMembershipPaymentsTemplate();

    IReadOnlyCollection<MembershipPaymentImportRow> ReadMembershipPayments(byte[] content);
}
