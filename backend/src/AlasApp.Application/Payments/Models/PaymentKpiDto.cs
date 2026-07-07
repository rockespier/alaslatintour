namespace AlasApp.Application.Payments.Models;

public sealed record PaymentKpiDto(
    decimal TotalRecaudadoMes,
    int TendenciaPercent,
    PaymentKpiBucketDto PagoPaypalConfirmados,
    PaymentKpiBeachBucketDto PagosPlayaValidados,
    PaymentKpiBucketDto MembresiasActivas);
