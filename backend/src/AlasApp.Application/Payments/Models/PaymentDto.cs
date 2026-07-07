using AlasApp.Domain.Enums;

namespace AlasApp.Application.Payments.Models;

public sealed record PaymentDto(
    Guid Id,
    DateTimeOffset Fecha,
    string Competidor,
    string Evento,
    string Categoria,
    decimal MontoUsd,
    PaymentMethod Metodo,
    string TransaccionId,
    PaymentStatusAdmin Estado,
    DateTimeOffset CreatedAt);
