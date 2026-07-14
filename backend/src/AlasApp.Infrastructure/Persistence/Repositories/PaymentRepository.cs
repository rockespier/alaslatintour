using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.Payments.Models;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AlasApp.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository(AlasAppDbContext dbContext) : IPaymentRepository
{
    public async Task<PagedResult<PaymentDto>> ListAsync(PaymentListFilter filter, CancellationToken cancellationToken)
    {
        var page = filter.Page <= 0 ? 1 : filter.Page;
        var limit = filter.Limit <= 0 ? 20 : filter.Limit;

        var query = BuildPaymentBaseQuery();

        if (filter.Method.HasValue)
            query = query.Where(x => x.Method == filter.Method.Value);

        if (filter.Status.HasValue)
            query = query.Where(x => x.Status == filter.Status.Value);

        if (filter.FromDate.HasValue)
        {
            var from = new DateTimeOffset(filter.FromDate.Value.Year, filter.FromDate.Value.Month, filter.FromDate.Value.Day, 0, 0, 0, TimeSpan.Zero);
            query = query.Where(x => x.Fecha >= from);
        }

        if (filter.ToDate.HasValue)
        {
            var toExclusive = new DateTimeOffset(filter.ToDate.Value.Year, filter.ToDate.Value.Month, filter.ToDate.Value.Day, 0, 0, 0, TimeSpan.Zero).AddDays(1);
            query = query.Where(x => x.Fecha < toExclusive);
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return new PagedResult<PaymentDto>(items.Select(MapToDto).ToList(), page, limit, totalItems);
    }

    public async Task<PaymentDto?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        var item = await BuildPaymentBaseQuery()
            .FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken);

        return item is null ? null : MapToDto(item);
    }

    public Task<Payment?> GetEntityByIdAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        return dbContext.Payments.FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken);
    }

    public Task<Payment?> GetEntityByInscriptionIdAsync(Guid inscriptionId, CancellationToken cancellationToken)
    {
        return dbContext.Payments.FirstOrDefaultAsync(x => x.InscriptionId == inscriptionId, cancellationToken);
    }

    public Task<bool> ExistsByTransactionIdAsync(string transactionId, CancellationToken cancellationToken)
    {
        var normalized = transactionId.Trim();
        return dbContext.Payments.AnyAsync(x => x.TransactionId == normalized, cancellationToken);
    }

    public Task AddAsync(Payment payment, CancellationToken cancellationToken)
    {
        return dbContext.Payments.AddAsync(payment, cancellationToken).AsTask();
    }

    public async Task<PaymentKpiDto> GetKpisAsync(DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        var currentMonthStart = new DateTimeOffset(utcNow.Year, utcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var nextMonthStart = currentMonthStart.AddMonths(1);
        var previousMonthStart = currentMonthStart.AddMonths(-1);

        var currentConfirmed = await dbContext.Payments
            .AsNoTracking()
            .Where(x => x.Status == PaymentStatusAdmin.Confirmado && x.Fecha >= currentMonthStart && x.Fecha < nextMonthStart)
            .ToListAsync(cancellationToken);

        var previousConfirmedAmount = await dbContext.Payments
            .AsNoTracking()
            .Where(x => x.Status == PaymentStatusAdmin.Confirmado && x.Fecha >= previousMonthStart && x.Fecha < currentMonthStart)
            .SumAsync(x => (decimal?)x.AmountUsd, cancellationToken) ?? 0m;

        var paypalConfirmed = currentConfirmed.Where(x => x.Method == PaymentMethod.Paypal).ToList();
        var beachConfirmed = currentConfirmed.Where(x => x.Method == PaymentMethod.Beach).ToList();

        var beachPendingCount = await dbContext.Payments
            .AsNoTracking()
            .CountAsync(
                x => x.Method == PaymentMethod.Beach
                    && x.Status == PaymentStatusAdmin.Pendiente
                    && x.Fecha >= currentMonthStart
                    && x.Fecha < nextMonthStart,
                cancellationToken);

        var totalRecaudadoMes = currentConfirmed.Sum(x => x.AmountUsd);
        var tendenciaPercent = previousConfirmedAmount == 0m
            ? (totalRecaudadoMes > 0m ? 100 : 0)
            : (int)Math.Round(((totalRecaudadoMes - previousConfirmedAmount) / previousConfirmedAmount) * 100m, MidpointRounding.AwayFromZero);

        return new PaymentKpiDto(
            totalRecaudadoMes,
            tendenciaPercent,
            new PaymentKpiBucketDto(paypalConfirmed.Sum(x => x.AmountUsd), paypalConfirmed.Count),
            new PaymentKpiBeachBucketDto(beachConfirmed.Sum(x => x.AmountUsd), beachConfirmed.Count, beachPendingCount),
            new PaymentKpiBucketDto(0m, 0));
    }

    private IQueryable<Payment> BuildPaymentBaseQuery()
    {
        return dbContext.Payments
            .AsNoTracking()
            .Include(x => x.Inscription).ThenInclude(i => i!.Competitor)
            .Include(x => x.Inscription).ThenInclude(i => i!.Event)
            .Include(x => x.Inscription).ThenInclude(i => i!.Category);
    }

    private static PaymentDto MapToDto(Payment x)
    {
        return new PaymentDto(
            x.Id,
            x.Fecha,
            $"{x.Inscription!.Competitor!.Nombre} {x.Inscription.Competitor.Apellido}",
            x.Inscription.Event!.Nombre,
            x.Inscription.Category!.Nombre,
            x.AmountUsd,
            x.Method,
            x.TransactionId,
            x.Status,
            x.CreatedAtUtc);
    }
}
