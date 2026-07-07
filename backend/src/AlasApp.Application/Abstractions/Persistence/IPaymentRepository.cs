using AlasApp.Application.Common;
using AlasApp.Application.Payments.Models;
using AlasApp.Domain.Entities;

namespace AlasApp.Application.Abstractions.Persistence;

public interface IPaymentRepository
{
    Task<PagedResult<PaymentDto>> ListAsync(PaymentListFilter filter, CancellationToken cancellationToken);

    Task<PaymentDto?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken);

    Task<Payment?> GetEntityByIdAsync(Guid paymentId, CancellationToken cancellationToken);

    Task<Payment?> GetEntityByInscriptionIdAsync(Guid inscriptionId, CancellationToken cancellationToken);

    Task<bool> ExistsByTransactionIdAsync(string transactionId, CancellationToken cancellationToken);

    Task AddAsync(Payment payment, CancellationToken cancellationToken);

    Task<PaymentKpiDto> GetKpisAsync(DateTimeOffset utcNow, CancellationToken cancellationToken);
}
