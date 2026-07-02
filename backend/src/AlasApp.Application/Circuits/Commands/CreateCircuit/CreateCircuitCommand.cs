using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Circuits.Models;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.Circuits.Commands.CreateCircuit;

public sealed record CreateCircuitCommand(
    string Nombre,
    int Temporada,
    string? Descripcion,
    CircuitRegion Region,
    CircuitModalidad Modalidad,
    CircuitStatus Estado,
    string? SurfScoresCode) : IRequest<CircuitDto>;
